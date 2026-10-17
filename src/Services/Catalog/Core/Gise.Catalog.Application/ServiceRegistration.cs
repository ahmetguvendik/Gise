using FluentValidation;
using Gise.Catalog.Application.Features.Handlers.Event.CommandHandlers;
using Gise.Catalog.Application.Features.Handlers.Event.QueryHandlers;
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

        services.AddValidatorsFromAssembly(typeof(ServiceRegistration).Assembly);

        return services;
    }
}
