namespace Gise.Contracts.Realtime;

// Koltuğun haritadaki durumu. Held: sepette, 10 dakika içinde satılmazsa geri açılır.
public enum SeatState
{
    Available,
    Held,
    Sold
}
