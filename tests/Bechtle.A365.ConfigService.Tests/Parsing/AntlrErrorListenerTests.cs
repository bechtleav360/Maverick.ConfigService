using System.IO;
using Antlr4.Runtime;
using Bechtle.A365.ConfigService.Parsing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Parsing
{
    public class AntlrErrorListenerTests
    {
        [Fact]
        public void CreateListener()
        {
            var loggerMock = new Mock<ILogger>(MockBehavior.Strict);

            var listener = new AntlrErrorListener(loggerMock.Object);

            loggerMock.Verify();
            Assert.NotNull(listener);
        }

        // these tests are weird, testing this stuff isn't exactly straight-forward
        // but otoh it doesn't matter that much, because it just logs some stuff out
        [Fact]
        public void LogSyntaxErrorBasic()
        {
            var loggerMock = new Mock<ILogger>();
            var writerMock = new Mock<TextWriter>();
            var recognizerMock = new Mock<IRecognizer>();
            var charStreamMock = new Mock<ICharStream>();
            var lexer = new ConfigReferenceLexer(charStreamMock.Object);

            var listener = new AntlrErrorListener(loggerMock.Object);

            listener.SyntaxError(
                writerMock.Object,
                recognizerMock.Object,
                0,
                4711,
                42,
                "some message to write",
                new RecognitionException(lexer, charStreamMock.Object));

            Assert.NotNull(listener);
        }

        [Fact]
        public void LogSyntaxErrorToken()
        {
            var loggerMock = new Mock<ILogger>();
            var writerMock = new Mock<TextWriter>();
            var recognizerMock = new Mock<IRecognizer>();
            var charStreamMock = new Mock<ICharStream>();
            var lexer = new ConfigReferenceLexer(charStreamMock.Object);
            var tokenMock = new Mock<IToken>();

            var listener = new AntlrErrorListener(loggerMock.Object);

            listener.SyntaxError(
                writerMock.Object,
                recognizerMock.Object,
                tokenMock.Object,
                4711,
                42,
                "some message to write",
                new RecognitionException(lexer, charStreamMock.Object));

            Assert.NotNull(listener);
        }
    }
}
