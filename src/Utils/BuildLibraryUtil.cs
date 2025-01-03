using Microsoft.Extensions.Logging;
using Soenneker.Git.Util.Abstract;
using Soenneker.Runners.Whisper.CTranslate.Utils.Abstract;
using Soenneker.Utils.Directory.Abstract;
using Soenneker.Utils.Process.Abstract;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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

        const string python = "python";

        _gitUtil.Clone("https://github.com/Softcatala/whisper-ctranslate2", tempDir);

        await _processUtil.Start(python, tempDir, "-m pip install --upgrade pip", waitForExit: true, cancellationToken: cancellationToken);

        await _processUtil.Start(python, tempDir, "-m pip install pip-review", waitForExit: true, cancellationToken: cancellationToken);

        await _processUtil.Start(python, tempDir, "-m site", waitForExit: true, cancellationToken: cancellationToken);

        await _processUtil.Start(python, tempDir, "-m pip-review --local --auto", waitForExit: true, cancellationToken: cancellationToken);

        await _processUtil.Start(python, tempDir, "-m pip install setuptools wheel build PyInstaller", waitForExit: true, cancellationToken: cancellationToken);

        await _processUtil.Start(python, tempDir, "-m pip install -r requirements.txt", waitForExit: true, cancellationToken: cancellationToken);

        await _processUtil.Start(python, tempDir, "setup.py sdist bdist_wheel", waitForExit: true, cancellationToken: cancellationToken);

        string scriptDir = Path.Combine(tempDir, "src", "whisper_ctranslate2");

        await _pythonImportConverter.ConvertRelativeImportsAsync(scriptDir);

        string entryScript = Path.Combine(scriptDir, "whisper_ctranslate2.py");

        await _processUtil.Start(python, tempDir, $"-m PyInstaller --onefile \"{entryScript}\"", waitForExit: true, cancellationToken: cancellationToken);

        return Path.Combine(tempDir, "dist", "whisper_ctranslate2.exe");
    }
}