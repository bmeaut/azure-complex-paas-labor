using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MyNewHome.Bll;
using Newtonsoft.Json;

namespace MyNewHome.Functions
{
    public static class NewPetFunction
    {
        private const string ComputerVisionUrl = "https://mynewhome-computervision.cognitiveservices.azure.com//vision/v1.0/generateThumbnail?width=400&height=300&smartCropping=true";

        [FunctionName("NewPetFunction")]
        public static async Task Run(
            [QueueTrigger("newpets", Connection = "StorageConnectionString")]string queueItem,
            ILogger logger,
            [ServiceLocator]IServiceProvider serviceProvider,
            [Config]IConfiguration config)
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var petService = serviceProvider.GetRequiredService<PetService>();

            var _storage = CloudStorageAccount.Parse(config.GetValue<string>("StorageConnectionString")).CreateCloudBlobClient();

            // Deserialize queue message
            var petFromQueue = JsonConvert.DeserializeObject<Pet>(queueItem);

            // Download image
            var blob = new CloudBlockBlob(new Uri(petFromQueue.ImageUrl), _storage);
            var image = await blob.DownloadBlobAsync();

            // Call Cognitive Services Computer Vision
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", config.GetValue<string>("ComputerVision"));
            var response = await client.PostAsync(ComputerVisionUrl, image);

            if (response.IsSuccessStatusCode)
            {
                // Upload image to blob
                var thumbnail = await response.Content.ReadAsStreamAsync();
                await blob.UploadFromStreamAsync(thumbnail);

                // Swap url host to CDN
                var url = new Uri(new Uri(config.GetValue<string>("ImageCdnHost")), blob.Uri.PathAndQuery).AbsoluteUri;

                // publish pet
                var pet = await petService.GetPetAsync(petFromQueue.Id, petFromQueue.Type);
                pet.ImageUrl = url;
                pet.Published = true;
                await petService.UpdatePetAsync(pet);
            }
            else
            {
                var result = await response.Content.ReadAsStringAsync();
                logger.LogError(result);
            }
        }

        static async Task<ByteArrayContent> DownloadBlobAsync(this CloudBlockBlob blob)
        {
            var stream = await blob.OpenReadAsync();

            var binaryReader = new BinaryReader(stream);
            var byteArray = binaryReader.ReadBytes((int)stream.Length);
            var content = new ByteArrayContent(byteArray);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            return content;
        }
    }
}
