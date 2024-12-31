using System.Threading.Tasks;

namespace Soenneker.Runners.Whisper.CTranslate.Utils.Abstract
{
    public interface IPythonImportConverter
    {
        Task ConvertRelativeImportsAsync(string directory);
    }
}
