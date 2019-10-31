using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MyNewHome.Bll;

namespace MyNewHome.Controllers
{
    [Route("api/pets")]
    [Produces("application/json")]
    [ApiController]
    public class PetController : ControllerBase
    {
        private readonly PetService _petService;
        private readonly CloudStorageAccount _storage;

        public PetController(PetService petService, IConfiguration configuration)
        {
            _petService = petService;
            _storage = CloudStorageAccount.Parse(configuration["StorageConnectionString"]);
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

            // TODO recognize pet type

            return Ok(new { url, type = "", probability = 0 });
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
