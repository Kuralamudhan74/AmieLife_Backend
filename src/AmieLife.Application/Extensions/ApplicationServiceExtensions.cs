using AmieLife.Application.Common.Interfaces;
using AmieLife.Application.Services;
using AmieLife.Application.Validators.Auth;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AmieLife.Application.Extensions;

/// <summary>
/// Registers all Application layer services into the DI container.
/// Called once from Program.cs — keeps startup clean.
/// </summary>
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();

        // Register all FluentValidation validators from this assembly
        services.AddValidatorsFromAssemblyContaining<SignupRequestValidator>();

        return services;
    }
}
