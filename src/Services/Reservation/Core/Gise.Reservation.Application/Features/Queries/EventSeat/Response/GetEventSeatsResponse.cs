namespace Gise.Reservation.Application.Features.Queries.EventSeat.Response;

public sealed class GetEventSeatsResponse
{
    public Guid EventId { get; set; }
    public DateTime StartsAt { get; set; }

    // İptal edilmemiş ve başlamamışsa satışta.
    public bool IsOnSale { get; set; }

    // Bütün koltuklar satılmış. Sepette tutulan koltuk satılmamış sayılır: 10 dakika içinde geri açılabilir.
    public bool SoldOut { get; set; }
    public List<SeatSectionResponse> Sections { get; set; } = [];
}

public sealed class SeatSectionResponse
{
    public Guid SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int AvailableCount { get; set; }
    public List<SeatResponse> Seats { get; set; } = [];
}

public sealed class SeatResponse
{
    public Guid SeatId { get; set; }
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }

    // Tutulmuş ya da satılmış koltuk ayrımı dışarıya verilmez.
    public bool IsAvailable { get; set; }
}
