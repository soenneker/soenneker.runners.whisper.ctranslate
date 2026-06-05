using System.Threading.Tasks;
using System.Threading;

namespace Soenneker.Runners.Whisper.CTranslate.Utils.Abstract;

/// <summary>
/// Defines the build library util contract.
/// </summary>
public interface IBuildLibraryUtil
{
    /// <summary>
    /// Executes the build operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the result of the operation.</returns>
    ValueTask<string> Build(CancellationToken cancellationToken);
}