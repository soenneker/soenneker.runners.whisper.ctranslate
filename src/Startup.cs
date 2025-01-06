using Microsoft.Extensions.DependencyInjection;
using Soenneker.Managers.Runners.Registrars;
using Soenneker.Runners.Whisper.CTranslate.Utils;
using Soenneker.Runners.Whisper.CTranslate.Utils.Abstract;

namespace Soenneker.Runners.Whisper.CTranslate;

/// <summary>
/// Console type startup
/// </summary>
public static class Startup
{
    // This method gets called by the runtime. Use this method to add services to the container.
    public static void ConfigureServices(IServiceCollection services)
    {
        services.SetupIoC();
    }

    public static IServiceCollection SetupIoC(this IServiceCollection services)
    {
        services.AddHostedService<ConsoleHostedService>();
        services.AddRunnersManagerAsScoped();
        services.AddScoped<IBuildLibraryUtil, BuildLibraryUtil>();
        services.AddScoped<IPythonImportConverter, PythonImportConverter>();

        return services;
    }
}
