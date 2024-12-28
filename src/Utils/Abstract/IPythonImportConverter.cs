using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soenneker.Runners.Whisper.CTranslate.Utils.Abstract
{
    public interface IPythonImportConverter
    {
        Task ConvertRelativeImportsAsync(string directory);
    }
}
