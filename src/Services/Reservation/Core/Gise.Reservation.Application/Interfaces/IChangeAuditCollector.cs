namespace Gise.Reservation.Application.Interfaces;

// Bir command'ın kaydettiği değişiklikler ("Event.Status: Draft → Published"). Persistence toplar, denetim satırı
// (CommandLoggingBehavior) boşaltır. İstek başına bir tane (scoped).
public interface IChangeAuditCollector
{
    void Add(string change);

    IReadOnlyList<string> Drain();
}
