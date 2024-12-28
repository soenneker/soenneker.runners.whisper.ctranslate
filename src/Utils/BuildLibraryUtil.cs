using Microsoft.Extensions.Logging;
using Soenneker.Extensions.Task;
using Soenneker.Git.Util.Abstract;
using Soenneker.Runners.Whisper.CTranslate.Utils.Abstract;
using Soenneker.Utils.Directory.Abstract;
using Soenneker.Utils.Process.Abstract;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Extensions.String;

namespace Soenneker.Runners.Whisper.CTranslate.Utils;

public class BuildLibraryUtil : IBuildLibraryUtil
{
    private readonly ILogger<BuildLibraryUtil> _logger;
    private readonly IGitUtil _gitUtil;
    private readonly IDirectoryUtil _directoryUtil;
    private readonly IProcessUtil _processUtil;
    private readonly IPythonImportConverter _pythonImportConverter;

    public BuildLibraryUtil(ILogger<BuildLibraryUtil> logger, IGitUtil gitUtil, IDirectoryUtil directoryUtil, IProcessUtil processUtil, IPythonImportConverter pythonImportConverter)
    {
        _logger = logger;
        _gitUtil = gitUtil;
        _directoryUtil = directoryUtil;
        _processUtil = processUtil;
        _pythonImportConverter = pythonImportConverter;
    }

    public async ValueTask<string> Build(CancellationToken cancellationToken)
    {
        string tempDir = _directoryUtil.CreateTempDirectory();

        string pythonDir = "python";

        _gitUtil.Clone("https://github.com/Softcatala/whisper-ctranslate2", tempDir);

        await StartProcess(pythonDir, tempDir, "-m pip install --upgrade pip", waitForExit: true, cancellationToken: cancellationToken);

        await StartProcess(pythonDir, tempDir, "-m pip install pip-review", waitForExit: true, cancellationToken: cancellationToken);

        await StartProcess(pythonDir, tempDir, "-m pip-review --local --auto", waitForExit: true, cancellationToken: cancellationToken);

        await StartProcess(pythonDir, tempDir, "-m pip install setuptools wheel build PyInstaller", waitForExit: true, cancellationToken: cancellationToken);

        await StartProcess(pythonDir, tempDir, "-m pip install -r requirements.txt", waitForExit: true, cancellationToken: cancellationToken);

        await StartProcess(pythonDir, tempDir, "setup.py sdist bdist_wheel", waitForExit: true, cancellationToken: cancellationToken);

        string scriptDir = Path.Combine(tempDir, "src", "whisper_ctranslate2");

        await _pythonImportConverter.ConvertRelativeImportsAsync(scriptDir);

        string entryScript = Path.Combine(scriptDir, "whisper_ctranslate2.py");

        await StartProcess(pythonDir, tempDir, $"-m PyInstaller --onefile \"{entryScript}\"", waitForExit: true, cancellationToken: cancellationToken);

        return Path.Combine(tempDir, "dist", "whisper_ctranslate2.exe");
    }

    public async ValueTask<List<string>> StartProcess(string name, string? directory = null, string? arguments = null, bool admin = false, bool waitForExit = false, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogInformation("Starting process ({name}) in directory ({directory}) with arguments ({arguments}) (admin? {admin}) (wait? {waitForExit}) ...", name, directory, arguments, admin, waitForExit);

        var processOutput = new List<string>();

        var startInfo = new ProcessStartInfo(name)
        {
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        if (arguments != null)
            startInfo.Arguments = arguments;

        if (directory != null)
            startInfo.WorkingDirectory = directory;

        if (admin)
            startInfo.Verb = "runas";

        var process = new System.Diagnostics.Process { StartInfo = startInfo };

        process.ErrorDataReceived += delegate (object _, DataReceivedEventArgs e) { OutputHandler(e, processOutput, log); };
        process.OutputDataReceived += delegate (object _, DataReceivedEventArgs e) { OutputHandler(e, processOutput, log); };

        process.Start();

        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        if (waitForExit)
        {
            if (log)
                _logger.LogDebug("Waiting for process ({process}) to end...", name);

            await process.WaitForExitAsync(cancellationToken).NoSync();
        }

        if (log)
            _logger.LogDebug("Process ({process}) has ended", name);

        return processOutput;
    }


    private void OutputHandler(DataReceivedEventArgs outLine, List<string> processOutput, bool log = true)
    {
        if (outLine.Data.IsNullOrEmpty())
            return;

        processOutput.Add(outLine.Data);

        if (log)
            _logger.LogInformation("{output}", outLine.Data);
    }
}