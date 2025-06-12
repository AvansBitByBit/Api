using Microsoft.AspNetCore.Mvc;
using BitByBitTrashAPI.Models;

namespace BitByBitTrashAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DummyTrashController : ControllerBase
    {
        private static readonly string[] TrashTypes = { "plastic", "organic", "paper", "glass", "restafval", "blik" };
        private static readonly string[] Locations = { 
            "Breda", "Avans", "Lovensdijkstraat", "Hogeschoollaan", 
            "naast de buurvrouw", "Park Valkenberg", "Grote Markt", 
            "Havermarkt", "Chasséveld", "Ginneken"
        };
        
        private static readonly List<TrashPickup> _dummyData = new();
        private static readonly Random _random = new();

        /// <summary>
        /// Get all dummy trash pickups (for testing frontend)
        /// </summary>
        [HttpGet]
        public ActionResult<IEnumerable<TrashPickup>> GetAll()
        {
            return Ok(_dummyData);
        }

        /// <summary>
        /// Get a specific number of randomized trash pickups
        /// </summary>
        [HttpGet("random/{count:int}")]
        public ActionResult<IEnumerable<TrashPickup>> GetRandomTrash(int count = 5)
        {
            if (count <= 0 || count > 100)
            {
                return BadRequest("Count must be between 1 and 100");
            }

            var randomTrash = Enumerable.Range(1, count).Select(index => GenerateRandomTrashPickup()).ToList();
            return Ok(randomTrash);
        }

        /// <summary>
        /// Get a single random trash pickup
        /// </summary>
        [HttpGet("random")]
        public ActionResult<TrashPickup> GetSingleRandomTrash()
        {
            var randomTrash = GenerateRandomTrashPickup();
            return Ok(randomTrash);
        }

        /// <summary>
        /// Add a new trash pickup (for testing frontend POST functionality)
        /// </summary>
        [HttpPost]
        public ActionResult<TrashPickup> PostTrash([FromBody] TrashPickup trash)
        {
            if (trash == null)
            {
                return BadRequest("Trash pickup data cannot be null");
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(trash.TrashType))
            {
                return BadRequest("TrashType is required");
            }

            // Generate ID if not provided
            if (trash.Id == null || trash.Id == Guid.Empty)
            {
                trash.Id = Guid.NewGuid();
            }

            // Set default time if not provided or in the past
            if (trash.Time == default || trash.Time < DateTime.Now.AddMinutes(-1))
            {
                trash.Time = DateTime.Now;
            }

            // Validate confidence score
            if (trash.Confidence < 0.0 || trash.Confidence > 1.0)
            {
                return BadRequest("Confidence must be between 0.0 and 1.0");
            }

            // Add to dummy data store
            _dummyData.Add(trash);

            return CreatedAtAction(nameof(GetById), new { id = trash.Id }, trash);
        }

        /// <summary>
        /// Get trash pickup by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        public ActionResult<TrashPickup> GetById(Guid id)
        {
            var trash = _dummyData.FirstOrDefault(t => t.Id == id);
            if (trash == null)
            {
                return NotFound($"Trash pickup with ID {id} not found");
            }

            return Ok(trash);
        }

        /// <summary>
        /// Update a trash pickup
        /// </summary>
        [HttpPut("{id:guid}")]
        public ActionResult<TrashPickup> UpdateTrash(Guid id, [FromBody] TrashPickup updatedTrash)
        {
            var existingTrash = _dummyData.FirstOrDefault(t => t.Id == id);
            if (existingTrash == null)
            {
                return NotFound($"Trash pickup with ID {id} not found");
            }

            // Update properties
            existingTrash.TrashType = updatedTrash.TrashType ?? existingTrash.TrashType;
            existingTrash.Location = updatedTrash.Location ?? existingTrash.Location;
            existingTrash.Confidence = updatedTrash.Confidence;
            existingTrash.Time = updatedTrash.Time != default ? updatedTrash.Time : existingTrash.Time;

            return Ok(existingTrash);
        }

        /// <summary>
        /// Delete a trash pickup
        /// </summary>
        [HttpDelete("{id:guid}")]
        public ActionResult DeleteTrash(Guid id)
        {
            var trash = _dummyData.FirstOrDefault(t => t.Id == id);
            if (trash == null)
            {
                return NotFound($"Trash pickup with ID {id} not found");
            }

            _dummyData.Remove(trash);
            return NoContent();
        }

        /// <summary>
        /// Clear all dummy data
        /// </summary>
        [HttpDelete("clear")]
        public ActionResult ClearAll()
        {
            _dummyData.Clear();
            return Ok("All dummy data cleared");
        }

        /// <summary>
        /// Seed the dummy data with random trash pickups
        /// </summary>
        [HttpPost("seed/{count:int}")]
        public ActionResult SeedData(int count = 10)
        {
            if (count <= 0 || count > 1000)
            {
                return BadRequest("Count must be between 1 and 1000");
            }

            _dummyData.Clear();
            for (int i = 0; i < count; i++)
            {
                _dummyData.Add(GenerateRandomTrashPickup());
            }

            return Ok($"Seeded {count} random trash pickups");
        }

        /// <summary>
        /// Get statistics about the dummy data
        /// </summary>
        [HttpGet("stats")]
        public ActionResult GetStats()
        {
            var stats = new
            {
                TotalCount = _dummyData.Count,
                TrashTypeBreakdown = _dummyData.GroupBy(t => t.TrashType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                LocationBreakdown = _dummyData.GroupBy(t => t.Location)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AverageConfidence = _dummyData.Any() ? _dummyData.Average(t => t.Confidence) : 0,
                LatestPickup = _dummyData.OrderByDescending(t => t.Time).FirstOrDefault()?.Time,
                OldestPickup = _dummyData.OrderBy(t => t.Time).FirstOrDefault()?.Time
            };

            return Ok(stats);
        }

        private static TrashPickup GenerateRandomTrashPickup()
        {
            return new TrashPickup
            {
                Id = Guid.NewGuid(),
                Time = DateTime.Now.AddMinutes(_random.Next(-10080, 0)), // Random time within the last week
                TrashType = TrashTypes[_random.Next(TrashTypes.Length)],
                Location = Locations[_random.Next(Locations.Length)],
                Confidence = Math.Round(_random.NextDouble(), 2) // Random confidence between 0.00 and 1.00
            };
        }
    }
}
