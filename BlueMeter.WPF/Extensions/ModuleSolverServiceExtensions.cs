using Microsoft.Extensions.DependencyInjection;
using BlueMeter.WPF.Services.ModuleSolver;

namespace BlueMeter.WPF.Extensions;

public static class ModuleSolverServiceExtensions
{
    public static IServiceCollection AddModuleSolverServices(this IServiceCollection services)
    {
        // Register ModuleSolver services as singletons for performance
        services.AddSingleton<PacketCaptureService>();
        services.AddSingleton<ModuleOptimizerService>();
        services.AddSingleton<ModulePersistenceService>();

        return services;
    }
}
