using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Api;

public static class ApiRegistration
{
    /// <summary>
    ///     HTTP behavior that every bounded context shares: problem details for failures and enums
    ///     that travel as strings, which keeps the API readable for the frontend.
    /// </summary>
    public static IServiceCollection AddCommonApi(this IServiceCollection services)
    {
        services.AddProblemDetails();

        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        return services;
    }

    public static WebApplication UseCommonApi(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        return app;
    }
}
