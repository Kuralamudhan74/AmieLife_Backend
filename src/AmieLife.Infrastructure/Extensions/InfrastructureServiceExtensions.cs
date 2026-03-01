using AmieLife.Application.Common.Interfaces;
using AmieLife.Infrastructure.Data;
using AmieLife.Infrastructure.Options;
using AmieLife.Infrastructure.Repositories;
using AmieLife.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AmieLife.Infrastructure.Extensions;

/// <summary>
/// Registers all Infrastructure layer dependencies: EF Core, repositories, and services.
/// Called once from Program.cs.
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database ────────────────────────────────────────────────────────
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured. " +
                "Set ConnectionStrings__DefaultConnection environment variable in production.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                // Retry on transient failures (cloud DB connections can hiccup)
                npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
            }));

        // ── Repositories ────────────────────────────────────────────────────
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

        // ── Services ────────────────────────────────────────────────────────
        services.AddScoped<ITokenService, TokenService>();

        // ── Email Service (conditional: real SMTP or console stub) ──────────
        var smtpEnabled = configuration.GetValue<bool>("Smtp:Enabled");

        if (smtpEnabled)
        {
            services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
            services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
            services.AddScoped<IEmailService, SmtpEmailService>();
        }
        else
        {
            services.AddScoped<IEmailService, ConsoleEmailService>();
        }

        return services;
    }
}
