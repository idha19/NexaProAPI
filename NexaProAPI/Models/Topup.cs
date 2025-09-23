using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexaProAPI.Models
{
    public class Topup
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty; //credit
        public DateTime TopupDate { get; set; }
    }
}
