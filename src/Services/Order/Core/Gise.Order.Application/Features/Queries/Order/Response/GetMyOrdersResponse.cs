namespace Gise.Order.Application.Features.Queries.Order.Response;

public sealed class GetMyOrdersResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int SeatCount { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
