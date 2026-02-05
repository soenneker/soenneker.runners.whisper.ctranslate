using Microsoft.Extensions.Logging;
using Soenneker.Git.Util.Abstract;
using Soenneker.Python.Utils.File.Abstract;
using Soenneker.Runners.Whisper.CTranslate.Utils.Abstract;
using Soenneker.Utils.Directory.Abstract;
using Soenneker.Utils.Process.Abstract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Python.Util.Abstract;
using Soenneker.Utils.File.Abstract;
using Soenneker.Extensions.String;
using Soenneker.Extensions.ValueTask;

namespace Soenneker.Runners.Whisper.CTranslate.Utils;

///<inheritdoc cref="IBuildLibraryUtil"/>
public sealed class BuildLibraryUtil : IBuildLibraryUtil
{
    private readonly ILogger<BuildLibraryUtil> _logger;
    private readonly IGitUtil _gitUtil;
    private readonly IDirectoryUtil _directoryUtil;
    private readonly IProcessUtil _processUtil;
    private readonly IPythonFileUtil _pythonFileUtil;
    private readonly IPythonUtil _pythonUtil;
    private readonly IFileUtil _fileUtil;

    // because build path matters
    private readonly string _gitDir = Path.Combine(Path.GetTempPath(), "81206ad6-b03f-4b46-a5c0-c637026409c5");
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "58b21c74-fb9e-4ffa-9541-54e3723a81d4");

    private const string _epochDate = "1640995200";

    private static readonly DateTime _epochUtc = DateTimeOffset.FromUnixTimeSeconds(_epochDate.ToInt()).UtcDateTime;

    // A dictionary of environment variables required for a reproducible PyInstaller build.
    private readonly Dictionary<string, string> _deterministicBuildEnvVars = new()
    {
        // SOURCE_DATE_EPOCH: A standard variable that many build tools respect. It sets a
        // fixed timestamp (Jan 1, 2022) for the build, removing timestamp-based randomness.
        {"SOURCE_DATE_EPOCH", _epochDate},

        // PYTHONHASHSEED: Setting this to '1' disables Python's hash randomization.
        // This ensures that dictionaries and sets have a consistent order, which is
        // critical for making sure PyInstaller packs files in the same order every time.
        {"PYTHONHASHSEED", "1"}
    };

    public BuildLibraryUtil(ILogger<BuildLibraryUtil> logger, IGitUtil gitUtil, IDirectoryUtil directoryUtil, IProcessUtil processUtil,
        IPythonFileUtil pythonFileUtil, IPythonUtil pythonUtil, IFileUtil fileUtil)
    {
        _logger = logger;
        _gitUtil = gitUtil;
        _directoryUtil = directoryUtil;
        _processUtil = processUtil;
        _pythonFileUtil = pythonFileUtil;
        _pythonUtil = pythonUtil;
        _fileUtil = fileUtil;
    }

    public async ValueTask<string> Build(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Setting environment variables for deterministic build...");
        foreach (KeyValuePair<string, string> kvp in _deterministicBuildEnvVars)
        {
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        }

        string python = await _pythonUtil.EnsureInstalled("3.12", true, cancellationToken);

        await _directoryUtil.CreateIfDoesNotExist(_gitDir, cancellationToken: cancellationToken);

        _logger.LogInformation("Python path: {path}", python);

        await _gitUtil.Clone("https://github.com/Softcatala/whisper-ctranslate2", _gitDir, cancellationToken: cancellationToken);

        await _directoryUtil.CreateIfDoesNotExist(_tempDir, cancellationToken: cancellationToken);

        await CopyDirectoryExceptGit(_gitDir, _tempDir, cancellationToken).NoSync();

        await _processUtil.Start(python, _tempDir, "-m pip install --upgrade pip", waitForExit: true, environmentalVars: _deterministicBuildEnvVars,
            cancellationToken: cancellationToken);

        await _processUtil.Start(python, _tempDir, "-m pip install pip-review", waitForExit: true, environmentalVars: _deterministicBuildEnvVars,
            cancellationToken: cancellationToken);

        await _processUtil.Start(python, _tempDir, "-m site", waitForExit: true, environmentalVars: _deterministicBuildEnvVars,
            cancellationToken: cancellationToken);

        await _processUtil.Start(python, _tempDir, "-m pip_review --local --auto", waitForExit: true, environmentalVars: _deterministicBuildEnvVars,
            cancellationToken: cancellationToken);

        await _processUtil.Start(python, _tempDir, "-m pip install setuptools wheel build PyInstaller", waitForExit: true,
            environmentalVars: _deterministicBuildEnvVars, cancellationToken: cancellationToken);

        await _processUtil.Start(python, _tempDir, "-m pip install -r requirements.txt", waitForExit: true, environmentalVars: _deterministicBuildEnvVars,
            cancellationToken: cancellationToken);

        await _processUtil.Start(python, _tempDir, "setup.py sdist bdist_wheel", waitForExit: true, environmentalVars: _deterministicBuildEnvVars,
            cancellationToken: cancellationToken);

        string scriptDir = Path.Combine(_tempDir, "src", "whisper_ctranslate2");

        await _pythonFileUtil.ConvertRelativeImports(scriptDir, cancellationToken);

        string entryScript = Path.Combine(scriptDir, "whisper_ctranslate2.py");

        _logger.LogInformation("Building executable with PyInstaller under a controlled environment...");

        string srcDir = Path.Combine(_tempDir, "src");
        var buildArgs = $"-m PyInstaller --onefile --clean --paths \"{srcDir}\" \"{entryScript}\"";

        await _processUtil.Start(python, _tempDir, buildArgs, waitForExit: true, environmentalVars: _deterministicBuildEnvVars,
            cancellationToken: cancellationToken);

        return Path.Combine(_tempDir, "dist", "whisper_ctranslate2.exe");
    }

    private async ValueTask CopyDirectoryExceptGit(string sourceDir, string destDir, CancellationToken cancellationToken = default)
    {
        // 1. Recreate directory tree
        foreach (string dir in Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            if (dir.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                continue;

            string targetDir = dir.Replace(sourceDir, destDir, StringComparison.Ordinal);
            await _directoryUtil.CreateIfDoesNotExist(targetDir, cancellationToken: cancellationToken);
        }

        // 2. Copy files
        foreach (string file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                continue;

            string targetFile = file.Replace(sourceDir, destDir, StringComparison.Ordinal);
            await _fileUtil.Copy(file, targetFile, true, cancellationToken).NoSync();

            // normalise timestamp so PyInstaller’s .pyc headers are consistent
            File.SetLastWriteTimeUtc(targetFile, _epochUtc);
        }
    }
}