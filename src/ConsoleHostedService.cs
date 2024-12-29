using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Soenneker.Config.Realtime.Abstract;
using Soenneker.Extensions.String;
using Soenneker.GitHub.Repositories.Releases.Abstract;
using Soenneker.Runners.Whisper.CTranslate.Utils.Abstract;
using Soenneker.Utils.Environment;

namespace Soenneker.Runners.Whisper.CTranslate;

public class ConsoleHostedService : IHostedService
{
    private readonly ILogger<ConsoleHostedService> _logger;

    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IFileOperationsUtil _fileOperationsUtil;
    private readonly IBuildLibraryUtil _buildLibraryUtil;
    private readonly IGitHubRepositoriesReleasesUtil _releasesUtil;
    private readonly IRealtimeConfigurationProvider _configProvider;

    private int? _exitCode;

    public ConsoleHostedService(ILogger<ConsoleHostedService> logger, IHostApplicationLifetime appLifetime,
        IFileOperationsUtil fileOperationsUtil, IBuildLibraryUtil buildLibraryUtil, IGitHubRepositoriesReleasesUtil releasesUtil, IRealtimeConfigurationProvider configProvider)
    {
        _logger = logger;
        _appLifetime = appLifetime;
        _fileOperationsUtil = fileOperationsUtil;
        _buildLibraryUtil = buildLibraryUtil;
        _releasesUtil = releasesUtil;
        _configProvider = configProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _appLifetime.ApplicationStarted.Register(() =>
        {
            Task.Run(async () =>
            {
                _logger.LogInformation("Running console hosted service ...");

                try
                {
                    string filePath = await _buildLibraryUtil.Build(cancellationToken);
                    await _fileOperationsUtil.Process(filePath, cancellationToken);

                    string username = EnvironmentUtil.GetVariableStrict("USERNAME");
                    string version = EnvironmentUtil.GetVariableStrict("BUILD_VERSION");

                    _configProvider.Set("GitHub:Username", username);
                    _configProvider.Set("GitHub:Token", EnvironmentUtil.GetVariableStrict("TOKEN"));

                    await _releasesUtil.Create(username, Constants.Library.ToLowerInvariantFast(), 
                        version, version, "Latest update", filePath, false, false,  cancellationToken);

                    _logger.LogInformation("Complete!");

                    _exitCode = 0;
                }
                catch (Exception e)
                {
                    if (Debugger.IsAttached)
                        Debugger.Break();

                    _logger.LogError(e, "Unhandled exception");

                    await Task.Delay(2000, cancellationToken);
                    _exitCode = 1;
                }
                finally
                {
                    // Stop the application once the work is done
                    _appLifetime.StopApplication();
                }
            }, cancellationToken);
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Exiting with return code: {exitCode}", _exitCode);

        // Exit code may be null if the user cancelled via Ctrl+C/SIGTERM
        Environment.ExitCode = _exitCode.GetValueOrDefault(-1);
        return Task.CompletedTask;
    }
}