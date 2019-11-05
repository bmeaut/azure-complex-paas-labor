using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using MyNewHome.Bll;

namespace MyNewHome.Controllers
{
    [Route("api/pets")]
    [Produces("application/json")]
    [ApiController]
    public class PetController : ControllerBase
    {
        private readonly PetService _petService;
        private readonly TelemetryClient _telemetryClient;
        private readonly CloudStorageAccount _storage;
        private readonly CustomVisionPredictionClient _customVision;
        private readonly Guid _customVisionId;

        public PetController(PetService petService, IConfiguration configuration, IHttpClientFactory httpClientFactory, TelemetryClient telemetryClient)
        {
            _petService = petService;
            _telemetryClient = telemetryClient;
            _storage = CloudStorageAccount.Parse(configuration["StorageConnectionString"]);

            _customVision = new CustomVisionPredictionClient(httpClientFactory.CreateClient(), false)
            {
                ApiKey = configuration["CustomVision:ApiKey"],
                Endpoint = configuration["CustomVision:Url"],
            };

            _customVisionId = new Guid(configuration["CustomVision:ProjectId"]);
        }

        [HttpGet]
        public async Task<IEnumerable<Pet>> GetPetsAsync()
        {
            return await _petService.ListPetsAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Pet>> PostPet([FromBody] Pet pet)
        {
            pet = await _petService.AddPetAsync(pet);

            // Retrieve a reference to a queue
            var queue = _storage.CreateCloudQueueClient().GetQueueReference("newpets");

            // Create the queue if it doesn't already exist
            await queue.CreateIfNotExistsAsync();

            // Create a message and add it to the queue
            var message = new CloudQueueMessage(pet.ToString());
            await queue.AddMessageAsync(message);

            return CreatedAtAction(nameof(GetPetsAsync), new { id = pet.Id }, pet);
        }

        [HttpPost("upload")]
        public async Task<ActionResult> UploadAndRecognizeImage()
        {
            var image = Request?.Form?.Files?[0];
            if (image == null) return BadRequest();

            // Retrieve a reference to a container
            var container = _storage.CreateCloudBlobClient().GetContainerReference("pets");

            // Create the container if it doesn't already exist
            await container.CreateIfNotExistsAsync();

            // Set container access level
            await container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Container });

            string ext = GetImageExtension(image.ContentType);
            if (ext == null) return BadRequest();

            // Upload image from stream with a generated filename
            var blob = container.GetBlockBlobReference(Guid.NewGuid().ToString() + "." + ext);
            await blob.UploadFromStreamAsync(image.OpenReadStream());

            var url = blob.Uri.AbsoluteUri;

            try
            {
                var prediction = await _customVision.ClassifyImageUrlAsync(_customVisionId, "Iteration2", new ImageUrl(url));
                var tag = prediction.Predictions.OrderByDescending(p => p.Probability).First();

                return Ok(new { url, type = tag.TagName, probability = tag.Probability });
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackException(ex);
                throw;
            }
        }

        private string GetImageExtension(string contentType)
        {
            switch (contentType)
            {
                case "image/png": return "png";
                case "image/jpeg": return "jpeg";
                case "image/jpg": return "jpg";
                case "image/gif": return "gif";
                case "image/bmp": return "bmp";
                case "image/ief": return "ief";
                case "image/svg+xml": return "svg+xml";
                case "image/raw": return "raw";
                default: return null;
            }
        }
    }
}
