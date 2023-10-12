using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EdgeTTS
{
    internal static class WebSocketExtension
    {
        public static Task SendAsync(
            this ClientWebSocket ws, string text, CancellationToken token = default)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var buffer = new ArraySegment<byte>(bytes, 0, bytes.Length);
            return ws.SendAsync(buffer, WebSocketMessageType.Text, true, token);
        }

        public static async Task<WebSocketReceiveData> ReceiveAsync(
            this ClientWebSocket ws, CancellationToken token = default)
        {
            var buffer = new ArraySegment<byte>(new byte[4096]);

            var ms = new MemoryStream();

            var result = null as WebSocketReceiveResult;

            do
            {
                result = await ws.ReceiveAsync(buffer, token);
                ms.Write(buffer.Array!, buffer.Offset, result.Count);
            }
            while (!result.EndOfMessage);

            ms.Seek(0, SeekOrigin.Begin);

            return new WebSocketReceiveData
            {
                MessageType = result.MessageType,
                Stream = ms
            };
        }
    }

    public class WebSocketReceiveData
    {
        public WebSocketMessageType MessageType { get; set; }
        public MemoryStream Stream { get; set; } = new MemoryStream();
    }
}
