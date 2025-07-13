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

    private const string _pyInstallerBootloaderKey = "4D3A1F8C2B5E9A6D7C0F1A9B3E5C8D4A";
    private string _tempDir = Path.Combine(Path.GetTempPath(), "58b21c74-fb9e-4ffa-9541-54e3723a81d4"); // because build path matters

    // A dictionary of environment variables required for a reproducible PyInstaller build.
    private static readonly Dictionary<string, string> _deterministicBuildEnvVars = new()
    {
        // SOURCE_DATE_EPOCH: A standard variable that many build tools respect. It sets a
        // fixed timestamp (Jan 1, 2022) for the build, removing timestamp-based randomness.
        {"SOURCE_DATE_EPOCH", "1640995200"},

        // PYTHONHASHSEED: Setting this to '0' disables Python's hash randomization.
        // This ensures that dictionaries and sets have a consistent order, which is
        // critical for making sure PyInstaller packs files in the same order every time.
        {"PYTHONHASHSEED", "0"}
    };

    public BuildLibraryUtil(ILogger<BuildLibraryUtil> logger, IGitUtil gitUtil, IDirectoryUtil directoryUtil, IProcessUtil processUtil,
        IPythonFileUtil pythonFileUtil, IPythonUtil pythonUtil)
    {
        _logger = logger;
        _gitUtil = gitUtil;
        _directoryUtil = directoryUtil;
        _processUtil = processUtil;
        _pythonFileUtil = pythonFileUtil;
        _pythonUtil = pythonUtil;
    }

    public async ValueTask<string> Build(CancellationToken cancellationToken)
    {
       _directoryUtil.CreateIfDoesNotExist(_tempDir);

        string python = await _pythonUtil.GetPythonPath(cancellationToken: cancellationToken);

        _logger.LogInformation("Python path: {path}", python);

        await _gitUtil.Clone("https://github.com/Softcatala/whisper-ctranslate2", _tempDir, cancellationToken: cancellationToken);

        await _processUtil.Start(python, _tempDir, "-m pip install --upgrade pip", waitForExit: true, cancellationToken: cancellationToken);

        await _processUtil.Start(python, _tempDir, "-m pip install pip-review", waitForExit: true, cancellationToken: cancellationToken);

        await _processUtil.Start(python, _tempDir, "-m site", waitForExit: true, cancellationToken: cancellationToken);

        await _processUtil.Start(python, _tempDir, "-m pip_review --local --auto", waitForExit: true, cancellationToken: cancellationToken);

        await _processUtil.Start(python, _tempDir, "-m pip install setuptools wheel build PyInstaller", waitForExit: true, cancellationToken: cancellationToken);

        await _processUtil.Start(python, _tempDir, "-m pip install -r requirements.txt", waitForExit: true, cancellationToken: cancellationToken);

        await _processUtil.Start(python, _tempDir, "setup.py sdist bdist_wheel", waitForExit: true, cancellationToken: cancellationToken);

        string scriptDir = Path.Combine(_tempDir, "src", "whisper_ctranslate2");

        await _pythonFileUtil.ConvertRelativeImports(scriptDir, cancellationToken);

        string entryScript = Path.Combine(scriptDir, "whisper_ctranslate2.py");

        _logger.LogInformation("Setting environment variables for deterministic build...");
        foreach (KeyValuePair<string, string> kvp in _deterministicBuildEnvVars)
        {
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        }

        _logger.LogInformation("Building executable with PyInstaller under a controlled environment...");

        var buildArgs =
            $"-m PyInstaller --onefile --key {_pyInstallerBootloaderKey} --clean --noupx \"{entryScript}\"";

        await _processUtil.Start(python, _tempDir, buildArgs, waitForExit: true, cancellationToken: cancellationToken);

        return Path.Combine(_tempDir, "dist", "whisper_ctranslate2.exe");
    }
}