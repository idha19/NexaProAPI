using System.ComponentModel.DataAnnotations.Schema;

namespace NexaProAPI.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; } //harga total
        public DateTime OrderDate { get; set; }
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

        //tambahan untuk status agar menambahkan fitur keranjang dan checkout
        public string Status { get; set; } = "Card"; // Cart → Pending → Approved/Rejected → Completed
    }
}
