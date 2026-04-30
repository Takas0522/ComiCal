using ComiCal.Application.UseCases.Account;
using ComiCal.Application.UseCases.Admin;
using ComiCal.Application.UseCases.Purchases;
using ComiCal.Application.UseCases.Series;
using ComiCal.Application.UseCases.Subscriptions;
using ComiCal.Application.UseCases.User;
using ComiCal.Application.UseCases.Volumes;
using ComiCal.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ComiCal.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Use cases
        services.AddScoped<SearchSeriesUseCase>();
        services.AddScoped<GetSeriesDetailUseCase>();
        services.AddScoped<GetUpcomingVolumesUseCase>();
        services.AddScoped<GetCalendarVolumesUseCase>();
        services.AddScoped<GetSubscriptionsUseCase>();
        services.AddScoped<AddSubscriptionUseCase>();
        services.AddScoped<RemoveSubscriptionUseCase>();
        services.AddScoped<UpdatePurchaseStateUseCase>();
        services.AddScoped<DeleteAccountUseCase>();
        services.AddScoped<GetBatchRunsUseCase>();
        services.AddScoped<ResolveUserUseCase>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<SearchSeriesRequestValidator>(ServiceLifetime.Singleton);

        return services;
    }
}
