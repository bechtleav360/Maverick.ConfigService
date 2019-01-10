using System.IO;
using Antlr4.Runtime;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Parsing
{
    public class AntlrErrorListener : IAntlrErrorListener<int>, IAntlrErrorListener<IToken>
    {
        private readonly ILogger _logger;

        /// <inheritdoc />
        public AntlrErrorListener(ILogger logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public void SyntaxError(TextWriter output,
                                IRecognizer recognizer,
                                int offendingSymbol,
                                int line,
                                int charPositionInLine,
                                string msg,
                                RecognitionException e)
        {
            _logger.LogError($"failed to tokenize '{e.InputStream}'[{line}:{charPositionInLine}]: {msg}");
        }

        /// <inheritdoc />
        public void SyntaxError(TextWriter output,
                                IRecognizer recognizer,
                                IToken offendingSymbol,
                                int line,
                                int charPositionInLine,
                                string msg,
                                RecognitionException e)
        {
            _logger.LogError($"failed to parse '{e.InputStream}'[{line}:{charPositionInLine}]: {msg}");
        }
    }
}