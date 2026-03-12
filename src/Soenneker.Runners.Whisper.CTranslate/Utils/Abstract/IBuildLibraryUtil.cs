using System.Threading.Tasks;
using System.Threading;

namespace Soenneker.Runners.Whisper.CTranslate.Utils.Abstract;

public interface IBuildLibraryUtil
{
    ValueTask<string> Build(CancellationToken cancellationToken);
}