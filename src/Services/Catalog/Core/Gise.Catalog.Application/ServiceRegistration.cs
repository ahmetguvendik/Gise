using FluentValidation;
using Gise.Catalog.Application.Features.Handlers.Event.CommandHandlers;
using Gise.Catalog.Application.Features.Handlers.Event.QueryHandlers;
using Gise.Catalog.Application.Features.Handlers.Venue.CommandHandlers;
using Gise.Catalog.Application.Features.Handlers.Venue.QueryHandlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Gise.Catalog.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<CreateEventCommandHandler>();
        services.AddScoped<GetAllEventsQueryHandler>();
        services.AddScoped<GetEventByIdQueryHandler>();

        services.AddScoped<CreateVenueCommandHandler>();
        services.AddScoped<GetAllVenuesQueryHandler>();
        services.AddScoped<GetVenueByIdQueryHandler>();

        services.AddValidatorsFromAssembly(typeof(ServiceRegistration).Assembly);

        return services;
    }
}
