using Microsoft.Extensions.Logging;
using Soenneker.Git.Util.Abstract;
using Soenneker.Python.Utils.File.Abstract;
using Soenneker.Runners.Whisper.CTranslate.Utils.Abstract;
using Soenneker.Utils.Directory.Abstract;
using Soenneker.Utils.Process.Abstract;
using System;
using System.Collections.Generic; 
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Extensions.Task;

namespace Soenneker.Runners.Whisper.CTranslate.Utils;

public sealed class BuildLibraryUtil : IBuildLibraryUtil
{
    private readonly ILogger<BuildLibraryUtil> _logger;
    private readonly IGitUtil _gitUtil;
    private readonly IDirectoryUtil _directoryUtil;
    private readonly IProcessUtil _processUtil;
    private readonly IPythonFileUtil _pythonFileUtil;

    private const string PyInstallerBootloaderKey = "4D3A1F8C2B5E9A6D7C0F1A9B3E5C8D4A";

    // A dictionary of environment variables required for a reproducible PyInstaller build.
    private static readonly Dictionary<string, string> DeterministicBuildEnvVars = new()
    {
        // SOURCE_DATE_EPOCH: A standard variable that many build tools respect. It sets a
        // fixed timestamp (Jan 1, 2022) for the build, removing timestamp-based randomness.
        {"SOURCE_DATE_EPOCH", "1640995200"},

        // PYTHONHASHSEED: Setting this to '0' disables Python's hash randomization.
        // This ensures that dictionaries and sets have a consistent order, which is
        // critical for making sure PyInstaller packs files in the same order every time.
        {"PYTHONHASHSEED", "0"},

        // PYINSTALLER_BOOTLOADER_KEY: Provides a fixed key to PyInstaller,
        // preventing it from generating a random one for each build.
        {"PYINSTALLER_BOOTLOADER_KEY", PyInstallerBootloaderKey}
    };

    public BuildLibraryUtil(ILogger<BuildLibraryUtil> logger, IGitUtil gitUtil, IDirectoryUtil directoryUtil, IProcessUtil processUtil,
        IPythonFileUtil pythonFileUtil)
    {
        _logger = logger;
        _gitUtil = gitUtil;
        _directoryUtil = directoryUtil;
        _processUtil = processUtil;
        _pythonFileUtil = pythonFileUtil;
    }

    public async ValueTask<string> Build(CancellationToken cancellationToken)
    {
        string tempDir = await _directoryUtil.CreateTempDirectory(cancellationToken);

        string python = await GetPythonPath(cancellationToken: cancellationToken);

        _logger.LogInformation("Python path: {path}", python);

        _gitUtil.Clone("https://github.com/Softcatala/whisper-ctranslate2", tempDir);

        await _processUtil.Start(python, tempDir, "-m pip install --upgrade pip", waitForExit: true, cancellationToken: cancellationToken);

        await _processUtil.Start(python, tempDir, "-m pip install pip-review", waitForExit: true, cancellationToken: cancellationToken);

        await _processUtil.Start(python, tempDir, "-m site", waitForExit: true, cancellationToken: cancellationToken);

        await _processUtil.Start(python, tempDir, "-m pip_review --local --auto", waitForExit: true, cancellationToken: cancellationToken);

        await _processUtil.Start(python, tempDir, "-m pip install setuptools wheel build PyInstaller", waitForExit: true, cancellationToken: cancellationToken);

        await _processUtil.Start(python, tempDir, "-m pip install -r requirements.txt", waitForExit: true, cancellationToken: cancellationToken);

        await _processUtil.Start(python, tempDir, "setup.py sdist bdist_wheel", waitForExit: true, cancellationToken: cancellationToken);

        string scriptDir = Path.Combine(tempDir, "src", "whisper_ctranslate2");

        await _pythonFileUtil.ConvertRelativeImports(scriptDir, cancellationToken);

        string entryScript = Path.Combine(scriptDir, "whisper_ctranslate2.py");

        _logger.LogInformation("Setting environment variables for deterministic build...");
        foreach (var kvp in DeterministicBuildEnvVars)
        {
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        }

        _logger.LogInformation("Building executable with PyInstaller under a controlled environment...");
        await _processUtil.Start(python, tempDir, $"-m PyInstaller --onefile \"{entryScript}\"", waitForExit: true, cancellationToken: cancellationToken);

        return Path.Combine(tempDir, "dist", "whisper_ctranslate2.exe");
    }

    public static async ValueTask<string> GetPythonPath(string pythonCommand = "python", CancellationToken cancellationToken = default)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = pythonCommand,
            Arguments = "-c \"import sys; print(sys.executable)\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = processStartInfo;
        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync(cancellationToken).NoSync();
        await process.WaitForExitAsync(cancellationToken).NoSync();

        return output.Trim();
    }
}