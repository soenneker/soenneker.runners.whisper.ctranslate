using System.Threading.Tasks;
using System.Threading;

namespace Soenneker.Runners.Whisper.CTranslate.Utils.Abstract;

/// <summary>
/// Builds the whisper-ctranslate2 Windows executable used by the target library.
/// </summary>
public interface IBuildLibraryUtil
{
    /// <summary>
    /// Creates a reproducible executable from the upstream whisper-ctranslate2 source.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The path to the generated executable.</returns>
    ValueTask<string> Build(CancellationToken cancellationToken);
}
