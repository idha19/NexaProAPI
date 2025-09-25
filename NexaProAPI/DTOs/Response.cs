namespace NexaProAPI.DTOs
{
    public class OrderResponseDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalPrice { get; set; } 
        public string Status { get; set; } = string.Empty;
        public List<OrderItemResponseDto> Items { get; set; } = new List<OrderItemResponseDto>();
    }

    public class OrderItemResponseDto
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Specification { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal SubPrice { get; set; }

        public List<CredentialDto> Credentials { get; set; } = new();
    }
}
