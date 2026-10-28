namespace Gise.Order.Application.Features.Queries.Order.Response;

public sealed class GetOrderByIdResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }

    // Pending (koltuklar tutuluyor), AwaitingPayment, Completed, Cancelled, Expired.
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemResponse> Items { get; set; } = [];
}

public sealed class OrderItemResponse
{
    public Guid SeatId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }
    public decimal Price { get; set; }
}
