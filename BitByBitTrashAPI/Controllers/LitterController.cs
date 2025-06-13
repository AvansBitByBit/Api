using Microsoft.AspNetCore.Mvc;
using BitByBitTrashAPI.Service;
using BitByBitTrashAPI.Models;
using Microsoft.AspNetCore.Authorization;


namespace BitByBitTrashAPI.Controllers
{
    [ApiController]
    [Route("Litter")]
    public class LitterController : ControllerBase
    {
        private readonly LitterDbContext _context;
        public LitterController(LitterDbContext context)
        {
            _context = context;
        }
        [Authorize(Roles = "Beheerder, IT, Gebruiker")]
        [HttpGet(Name = "GetLitter")]
        public IEnumerable<TrashPickup> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new TrashPickup
            {
                Id = Guid.NewGuid(),
                TrashType = new[] { "cola", "blikje", "fles", "plastic", "organisch" }[new Random().Next(0, 5)],
                Location = new[] { "Breda", "Avans", "Lovensdijkstraat", "Hogeschoollaan", "naast de buurvrouw" }[new Random().Next(0, 5)],
                Confidence = Math.Round(new Random().NextDouble(), 2) // Example confidence value between 0.00 and 1.00
                
            });
        }
        [Authorize(Roles = "Beheerder, IT")]
        [HttpPost(Name = "PostLitter")]
        public IActionResult Post([FromBody] TrashPickup litter)
        {
            if (litter == null)
            {
                return BadRequest("Litter cannot be null");
            }

            return Ok("Het is gelukt");
        }


    }
}
