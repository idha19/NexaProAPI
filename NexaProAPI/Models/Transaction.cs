using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexaProAPI.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty; //topup, purchace order, income order
        public DateTime TransactionDate { get; set; }
    }
}
