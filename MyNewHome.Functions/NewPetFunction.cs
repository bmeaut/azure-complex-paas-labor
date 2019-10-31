using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MyNewHome.ClassLibrary;
using Newtonsoft.Json;

namespace MyNewHome.Functions
{
    public static class NewPetFunction
    {
        private static readonly string _cosmosConnectionString = Environment.GetEnvironmentVariable("CosmosConnectionString");
        private static readonly string _computerVisionApiKey = Environment.GetEnvironmentVariable("ComputerVision");
        private static readonly string _storageConnectionString = Environment.GetEnvironmentVariable("StorageConnectionString");

        private static readonly PetService _petService = new PetService(_cosmosConnectionString);
        private static readonly HttpClient _client = new HttpClient();
        private static readonly CloudBlobClient _storage = CloudStorageAccount.Parse(_storageConnectionString).CreateCloudBlobClient();

        [FunctionName("NewPetFunction")]
        public static async Task Run([QueueTrigger("newpets", Connection = "StorageConnectionString")]string queueItem, ILogger logger)
        {
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
            _client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _computerVisionApiKey);
            var response = await _client.PostAsync(
                "https://westeurope.api.cognitive.microsoft.com/vision/v1.0/generateThumbnail?width=400&height=300&smartCropping=true",
                content);

            if (response.IsSuccessStatusCode)
            {
                // Upload image to blob
                var thumbnail = await response.Content.ReadAsStreamAsync();
                await blob.UploadFromStreamAsync(thumbnail);

                // Save url to Cosmos DB and publish
                var pet = await _petService.GetPetAsync(petFromQueue.Id, petFromQueue.Type);
                pet.Published = true;
                await _petService.UpdatePetAsync(pet);
            }
            else
            {
                var result = await response.Content.ReadAsStringAsync();
                logger.LogError(result);
            }
        }
    }
}
