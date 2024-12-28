using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Soenneker.Runners.Whisper.CTranslate.Utils.Abstract;

namespace Soenneker.Runners.Whisper.CTranslate.Utils
{
    public class PythonImportConverter : IPythonImportConverter
    {
        private readonly ILogger<PythonImportConverter> _logger;

        public PythonImportConverter(ILogger<PythonImportConverter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Converts all relative imports to absolute imports in Python scripts within the specified directory.
        /// </summary>
        /// <param name="directory">The root directory containing Python scripts.</param>
        public async Task ConvertRelativeImportsAsync(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("Directory cannot be null or empty.", nameof(directory));

            if (!Directory.Exists(directory))
            {
                _logger.LogError("Directory {Directory} does not exist.", directory);
                throw new DirectoryNotFoundException($"Directory {directory} does not exist.");
            }

            string[] pythonFiles = Directory.GetFiles(directory, "*.py", SearchOption.AllDirectories);

            foreach (string scriptPath in pythonFiles)
            {
                _logger.LogInformation("Processing: {ScriptPath}", scriptPath);

                try
                {
                    // Read all lines from the Python file
                    string[] lines = await File.ReadAllLinesAsync(scriptPath).ConfigureAwait(false);
                    bool isModified = false;

                    for (int i = 0; i < lines.Length; i++)
                    {
                        string originalLine = lines[i];
                        string trimmedLine = originalLine.TrimStart();

                        // Check if the line starts with 'from .' indicating a relative import
                        if (trimmedLine.StartsWith("from ."))
                        {
                            string modifiedLine = RemoveLeadingDotsFromImport(trimmedLine);

                            if (modifiedLine != trimmedLine)
                            {
                                // Preserve the original indentation
                                int leadingWhitespaceCount = originalLine.Length - trimmedLine.Length;
                                string indentation = originalLine.Substring(0, leadingWhitespaceCount);
                                lines[i] = indentation + modifiedLine;
                                isModified = true;

                                _logger.LogDebug("Modified line in {ScriptPath}: \"{Original}\" -> \"{Modified}\"", scriptPath, trimmedLine, modifiedLine);
                            }
                        }
                    }

                    // Only write back to the file if modifications were made
                    if (isModified)
                    {
                        await File.WriteAllLinesAsync(scriptPath, lines).ConfigureAwait(false);
                        _logger.LogInformation("Updated: {ScriptPath}", scriptPath);
                    }
                    else
                    {
                        _logger.LogInformation("No changes needed for: {ScriptPath}", scriptPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing file {ScriptPath}", scriptPath);
                }
            }
        }

        /// <summary>
        /// Removes leading dots from a relative import statement.
        /// For example, converts 'from ..module import something' to 'from module import something'.
        /// </summary>
        /// <param name="importLine">The import line to modify.</param>
        /// <returns>The modified import line with leading dots removed.</returns>
        private string RemoveLeadingDotsFromImport(string importLine)
        {
            // Split the line into 'from', module path with dots, 'import', and imported items
            // Example: 'from ..module import something' -> ['from', '..module', 'import', 'something']
            string[] parts = importLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 4 || parts[0] != "from" || parts[2] != "import")
            {
                // Not a valid 'from ... import ...' statement
                return importLine;
            }

            string modulePathWithDots = parts[1];
            string importedItems = string.Join(" ", parts, 3, parts.Length - 3);

            // Remove all leading dots from the module path
            string modulePath = modulePathWithDots.TrimStart('.');

            // Handle cases where modulePath might be empty (e.g., 'from . import something')
            if (string.IsNullOrEmpty(modulePath))
            {
                // Convert to 'import something' by removing 'from .'
                return $"import {importedItems}";
            }

            return $"from {modulePath} import {importedItems}";
        }
    }
}
