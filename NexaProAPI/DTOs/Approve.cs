namespace NexaProAPI.DTOs
{
    // Credential DTO
    public class CredentialDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }


    // Delivery DTO per OrderItem
    public class DeliveryDto
    {
        public int OrderItemId { get; set; }
        public List<CredentialDto> Credentials { get; set; } = new();
    }


    // ApproveOrder DTO (incoming request for admin)
    public class ApproveOrderDto
    {
        public int OrderId { get; set; }
        public List<DeliveryDto> Deliveries { get; set; } = new();
    }


}
