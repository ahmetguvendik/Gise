using Gise.Identity.Application.Interfaces;

namespace Gise.Identity.Application.Auditing;

// Telgraf'taki gibi bellekte: tek worker varsayar ve yeniden başlatmada sıfırlanır (ilk tur imleci o ana kurar). Worker
// kapalıyken olan olaylar denetim loguna girmez, Keycloak'ta durur. Worker çoğaltılırsa ya da boşluk kabul edilemezse
// Redis'e taşınmalı.
public sealed class InMemoryKeycloakEventCursor : IKeycloakEventCursor
{
    private KeycloakEventPosition? _position;

    public ValueTask<KeycloakEventPosition?> ReadAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(_position);

    public ValueTask WriteAsync(KeycloakEventPosition position, CancellationToken cancellationToken = default)
    {
        _position = position;
        return ValueTask.CompletedTask;
    }
}
