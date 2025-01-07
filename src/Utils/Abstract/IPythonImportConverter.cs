using System.Threading.Tasks;

namespace Soenneker.Runners.Whisper.CTranslate.Utils.Abstract;

public interface IPythonImportConverter
{
    ValueTask ConvertRelativeImports(string directory);
}