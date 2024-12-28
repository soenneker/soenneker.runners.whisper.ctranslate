using Microsoft.Extensions.Logging;
using Soenneker.Git.Util.Abstract;
using Soenneker.Runners.Whisper.CTranslate.Utils.Abstract;
using Soenneker.Utils.Directory.Abstract;
using Soenneker.Utils.Process.Abstract;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Runners.Whisper.CTranslate.Utils;

public class BuildLibraryUtil : IBuildLibraryUtil
{
    private readonly ILogger<BuildLibraryUtil> _logger;
    private readonly IGitUtil _gitUtil;
    private readonly IDirectoryUtil _directoryUtil;
    private readonly IProcessUtil _processUtil;

    public BuildLibraryUtil(ILogger<BuildLibraryUtil> logger, IGitUtil gitUtil, IDirectoryUtil directoryUtil, IProcessUtil processUtil)
    {
        _logger = logger;
        _gitUtil = gitUtil;
        _directoryUtil = directoryUtil;
        _processUtil = processUtil;
    }

    public async ValueTask<string> Build(CancellationToken cancellationToken)
    {
        string tempDir = _directoryUtil.CreateTempDirectory();

        _gitUtil.Clone("https://github.com/Softcatala/whisper-ctranslate2", tempDir);

        await _processUtil.StartProcess("python.exe", tempDir, "-m ensurepip --upgrade", cancellationToken: cancellationToken);

        await _processUtil.StartProcess("python.exe", tempDir, "pip install setuptools wheel build pyinstaller", cancellationToken: cancellationToken);

        await _processUtil.StartProcess("python.exe", tempDir, "pip install -r requirements.txt", cancellationToken: cancellationToken);

        await _processUtil.StartProcess("python.exe", tempDir, "setup.py sdist bdist_wheel", cancellationToken: cancellationToken);

        return null;
    }
}