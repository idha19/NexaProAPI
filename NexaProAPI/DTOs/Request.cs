namespace NexaProAPI.DTOs
{
    public class CreateOrderDto
    {
        public List<CreateOrderItemDto> Items { get; set; }
    }

    public class CreateOrderItemDto
    {
        public int AccountId { get; set; }
        public int Quantity { get; set; }
    }
    public class UpdateOrderItemDto
    {
        public int Quantity { get; set; }
        public decimal SubPrice { get; set; }
        public int? AccountId { get; set; }
    }
}
