
using Microsoft.AspNetCore.Mvc;
using test1.Service;


namespace test1.api
{
    [ApiController]
    [Route("Litter")]
    public class Litter : ControllerBase
    {
        private readonly LitterDbContext _context;
        public Litter(LitterDbContext context)
        {
            _context = context;
        }

        [HttpGet(Name = "GetLitter")]
        public IEnumerable<models.LitterModel> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new models.LitterModel
            {
                Id = 2,
                Name = "Litter",
                Type = "Description"
            });
        }

        [HttpPost(Name = "PostLitter")]
        public IActionResult Post([FromBody] models.LitterModel litter)
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
