using System.ComponentModel.DataAnnotations;

namespace BitByBitTrashAPI.Models
{
    public class TrashPickup
    {
        public Guid? Id { get; set; } // nullable omdat zij waarschijnlijk de ID mee geven?
        public DateTime Time { get; set; } = DateTime.Now; // default to current time if no value is provided <-- mag niet in het verleden liggen bij invoer?? idk dit stond er ofzo
  
        [Required] public string TrashType { get; set; } // verplichte veld
        
        public string Location { get; set; }
        public double Confidence { get; set; } // Confidence score for the pickup
        
    }
}
