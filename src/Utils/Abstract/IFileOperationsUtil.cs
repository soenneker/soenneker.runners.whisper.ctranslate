using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Runners.Whisper.CTranslate.Utils.Abstract;

public interface IFileOperationsUtil
{
    ValueTask Process(string filePath, CancellationToken cancellationToken);
}
