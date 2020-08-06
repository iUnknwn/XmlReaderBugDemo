using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

namespace XmlReaderIssueDemo
{
    public class XmlReaderTest : IDisposable
    {
        private AnonymousPipeServerStream pipeServer;
        private AnonymousPipeClientStream pipeClient;

        public XmlReaderTest()
        {
            pipeServer = new AnonymousPipeServerStream(PipeDirection.In);
        }

        public void Dispose()
        {
            pipeServer?.Dispose();
            pipeClient?.Dispose();
        }

        private void WriteSinglElementMessage_AndClosePipe()
        {
            using (var client = new AnonymousPipeClientStream(PipeDirection.Out, pipeServer.ClientSafePipeHandle))
            {
                var writer = XmlWriter.Create(client);
                writer.WriteElementString("Element", "Value");
                writer.Flush();
                writer.Close();
            }
            
        }

        private void WriteSinglElementMessage_AndLeavePipeOpen()
        {
            pipeClient = new AnonymousPipeClientStream(PipeDirection.Out, pipeServer.ClientSafePipeHandle);
            var writer = XmlWriter.Create(pipeClient);
            writer.WriteElementString("Element", "Value");
            writer.Flush();
            writer.Close();
        }

        [Fact]
        public void ReadValues_ReaderRead_ClosedPipe()
        {
            WriteSinglElementMessage_AndClosePipe();

            var reader = XmlReader.Create(pipeServer);
            reader.MoveToContent();

            Assert.Equal("Element", reader.LocalName);

            reader.Read();

            Assert.Equal("Value", reader.Value);
        }

        [Fact]
        public void ReadValues_ReadElementString_ClosedPipe()
        {
            WriteSinglElementMessage_AndClosePipe();

            var reader = XmlReader.Create(pipeServer);
            reader.MoveToContent();
            Assert.Equal("Element", reader.LocalName);

            //this works fine, since the pipe was closed
            var nodeValue = reader.ReadElementContentAsString();

            Assert.Equal("Value", nodeValue);
        }

        [Fact]
        public void ReadValues_ReaderRead_OpenPipePipe()
        {
            WriteSinglElementMessage_AndLeavePipeOpen();

            var reader = XmlReader.Create(pipeServer);
            reader.MoveToContent();

            Assert.Equal("Element", reader.LocalName);

            reader.Read();

            Assert.Equal("Value", reader.Value);
        }

        [Fact(Timeout = 1000)]
        public async Task ReadValues_ReadElementString_OpenPipe()
        {
            WriteSinglElementMessage_AndLeavePipeOpen();

            var reader = XmlReader.Create(pipeServer, new XmlReaderSettings {Async = true});
            reader.MoveToContent();
            Assert.Equal("Element", reader.LocalName);

            // hang occurs here - the pipe is open, so more data is possible,
            // but the xml reader doesn't realize there is a single XML element
            // document, so it waits because it wants to go PAST the current element.

            // note - this also hangs when using the non-async variant, but xunit's
            // timeout function doesn't detect it (see xunit #217)
            var nodeValue = await reader.ReadElementContentAsStringAsync();

            Assert.Equal("Value", nodeValue);
        }
    }
}
