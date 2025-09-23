using System.ComponentModel.DataAnnotations.Schema;

namespace NexaProAPI.Models
{
    public class Account
    {
        public int Id { get; set; }
        public string Thumbnail { get; set; } = string.Empty;
        public string Specification { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }
        public int Count { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
    }
}
