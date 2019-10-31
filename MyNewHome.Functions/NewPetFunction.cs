using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MyNewHome.Bll;
using Newtonsoft.Json;

namespace MyNewHome.Functions
{
    public static class NewPetFunction
    {
        [FunctionName("NewPetFunction")]
        public static async Task Run(
            [QueueTrigger("newpets", Connection = "StorageConnectionString")]string queueItem,
            ILogger logger,
            PetService petService,
            HttpClient client,
            [Config]IConfiguration config)
        {
            var _storage = CloudStorageAccount.Parse(config.GetValue<string>("StorageConnectionString")).CreateCloudBlobClient();

            // Deserialize queue message
            var petFromQueue = JsonConvert.DeserializeObject<Pet>(queueItem);

            // Download image
            var blob = new CloudBlockBlob(new Uri(petFromQueue.ImageUrl), _storage);
            var stream = await blob.OpenReadAsync();

            var binaryReader = new BinaryReader(stream);
            var byteArray = binaryReader.ReadBytes((int)stream.Length);
            var content = new ByteArrayContent(byteArray);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            // Call Cognitive Services Computer Vision
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", config.GetValue<string>("ComputerVision"));
            var response = await client.PostAsync(
                "https://westeurope.api.cognitive.microsoft.com/vision/v1.0/generateThumbnail?width=400&height=300&smartCropping=true",
                content);

            if (response.IsSuccessStatusCode)
            {
                // Upload image to blob
                var thumbnail = await response.Content.ReadAsStreamAsync();
                await blob.UploadFromStreamAsync(thumbnail);

                // publish pet
                var pet = await petService.GetPetAsync(petFromQueue.Id, petFromQueue.Type);
                pet.Published = true;
                await petService.UpdatePetAsync(pet);
            }
            else
            {
                var result = await response.Content.ReadAsStringAsync();
                logger.LogError(result);
            }
        }
    }
}
