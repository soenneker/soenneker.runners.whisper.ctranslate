using Microsoft.Extensions.DependencyInjection;
using Soenneker.Git.Util.Registrars;
using Soenneker.GitHub.Repositories.Releases.Registrars;
using Soenneker.Runners.Whisper.CTranslate.Utils;
using Soenneker.Runners.Whisper.CTranslate.Utils.Abstract;
using Soenneker.Utils.Dotnet.NuGet.Registrars;
using Soenneker.Utils.File.Registrars;
using Soenneker.Utils.FileSync.Registrars;
using Soenneker.Utils.SHA3.Registrars;

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
        services.AddSha3UtilAsScoped();
        services.AddFileUtilSyncAsScoped();
        services.AddGitUtilAsScoped();
        services.AddScoped<IFileOperationsUtil, FileOperationsUtil>();
        services.AddDotnetNuGetUtilAsScoped();
        services.AddScoped<IBuildLibraryUtil, BuildLibraryUtil>();
        services.AddFileUtilAsScoped();
        services.AddScoped<IPythonImportConverter, PythonImportConverter>();
        services.AddGitHubRepositoriesReleasesUtilAsScoped();

        return services;
    }
}
