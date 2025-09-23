using System.ComponentModel.DataAnnotations;

namespace NexaProAPI.Models
{

    public enum ProductCategory
    {
        Software_editing,
        Streaming,
    }
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        public string Logo { get; set; } = string.Empty;
        public ProductCategory Category { get; set; }
    }
}
