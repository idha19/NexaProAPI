using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexaProAPI.Models
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal SubPrice { get; set; } //harga asli
        public int Quantity { get; set; }

        [Required]
        public int AccountId { get; set; }
        public Account? Account { get; set; }

        // Relasi ke DeliveryCredential
        public ICollection<DeliveryCredential> Credentials { get; set; } = new List<DeliveryCredential>();
    }
}
