using System.ComponentModel.DataAnnotations;

namespace BitByBitTrashAPI.Models
{
    public class TrashPickup
    {
        public Guid? Id { get; set; } // nullable omdat zij waarschijnlijk de ID mee geven?
        public DateTime Time { get; set; } = DateTime.Now; // default to current time if no value is provided <-- mag niet in het verleden liggen bij invoer?? idk dit stond er ofzo
  
        [Required] public string TrashType { get; set; } // verplichte veld > mogelijke waarden: "plastic", "organic", "paper", "glass", "restafval, blik"
        
        public string Location { get; set; } // coordinaten in breda Ongeveer: Noordwesthoek: 51.622° N, 4.704° EZuidoosthoek: 51.547° N, 4.836° EDat betekent dat Breda ligt tussen:Breedtegraad (latitude): van 51.547° tot 51.622°Lengtegraad (longitude): van 4.704° tot 4.836°
        public double Confidence { get; set; } // Confidence score for the pickup (0.0 to 1.0)
        
        public double? Temperature { get; set; } // Temperature at the time of pickup (Celsius)
    }
}
