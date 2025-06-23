using System.ComponentModel.DataAnnotations;

namespace BitByBitTrashAPI.Models
{
    public class TrashPickup
    {
        public Guid? Id { get; set; } // nullable omdat zij waarschijnlijk de ID mee geven?
        public DateTime Time { get; set; } = DateTime.Now; // default to current time if no value is provided <-- mag niet in het verleden liggen bij invoer?? idk dit stond er ofzo
  
        [Required] public string TrashType { get; set; } = string.Empty; // verplichte veld > mogelijke waarden: "plastic", "organic", "paper", "glass", "restafval, blik"
        
        public string Location { get; set; } = string.Empty; // Address in Breda area
        public double? Latitude { get; set; } // Latitude coordinate for the location
        public double? Longitude { get; set; } // Longitude coordinate for the location
        public double Confidence { get; set; } // Confidence score for the pickup (0.0 to 1.0)
        
        public double? Temperature { get; set; } // Historical temperature at the time and location of pickup (Celsius)
    }
}
