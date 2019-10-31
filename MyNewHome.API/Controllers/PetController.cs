using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyNewHome.ClassLibrary;

namespace MyNewHome.Controllers
{
    [Route("api/pets")]
    [Produces("application/json")]
    [ApiController]
    public class PetController : ControllerBase
    {
        private readonly PetService _petService;

        public PetController(PetService petService)
        {
            _petService = petService;
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
            // TODO save to blob
            // TODO recognize pet type

            throw new NotImplementedException();
        }
    }
}
