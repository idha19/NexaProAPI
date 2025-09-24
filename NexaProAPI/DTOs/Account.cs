namespace NexaProAPI.DTOs
{
    public class AccountDto
    {
        public int Id { get; set; }
        public string Thumbnail { get; set; } = string.Empty;
        public string Specification { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Count { get; set; }
        public int ProductId { get; set; }
    }

    public class CreateAccountDto
    {
        public string Thumbnail { get; set; } = string.Empty;
        public string Specification { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Count { get; set; }
        public int ProductId { get; set; }
    }

    public class UpdateAccountDto
    {
        public string Thumbnail { get; set; } = string.Empty;
        public string Specification { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Count { get; set; }
    }
}