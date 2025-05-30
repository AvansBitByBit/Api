
using Microsoft.AspNetCore.Mvc;
using BitByBitTrashAPI.Service;
using BitByBitTrashAPI.Models;


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

        [HttpGet(Name = "GetLitter")]
        public IEnumerable<LitterModel> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new LitterModel
            {
                Id = 2,
                Name = "Litter",
                Type = "Description"
            });
        }

        [HttpPost(Name = "PostLitter")]
        public IActionResult Post([FromBody] LitterModel litter)
        {
            if (litter == null)
            {
                return BadRequest("Litter cannot be null");
            }
            // Save the litter to the database or perform any other action
            return Ok("Het is gelukt");
        }


    }
}
