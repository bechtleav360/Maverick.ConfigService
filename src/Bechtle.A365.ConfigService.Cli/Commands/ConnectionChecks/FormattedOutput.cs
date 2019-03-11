using System.Text;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Cli.Commands.ConnectionChecks
{
    public class FormattedOutput
    {
        public ILogger Logger { get; }

        /// <inheritdoc />
        public FormattedOutput(ILogger logger)
        {
            Logger = logger;
        }

        private readonly object _indentBuilderLock = new object();

        private readonly StringBuilder _indentBuilder = new StringBuilder();

        private string GetIndent(int level)
        {
            lock (_indentBuilderLock)
            {
                // to be extra safe there is nothing in there...
                _indentBuilder.Clear();

                for (var i = 0; i < level; ++i)
                    _indentBuilder.Append("| ");

                var result = _indentBuilder.ToString();

                _indentBuilder.Clear();

                return result;
            }
        }

        public void Line(string line, int level = 0, bool verbose = false)
        {
            if (level > 0)
            {
                Line(GetIndent(level) + line, 0, verbose);
                return;
            }

            if (verbose)
                Logger.LogDebug(line);
            else
                Logger.LogInformation(line);
        }

        public void Line(int level) => Line(string.Empty, level);

        public void Separator()
        {
            Logger.LogInformation("");
            Logger.LogInformation("------------------------------------");
            Logger.LogInformation("");
        }
    }
}