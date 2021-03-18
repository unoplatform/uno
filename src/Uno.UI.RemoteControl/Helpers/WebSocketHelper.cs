using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl.Helpers
{
	public static class WebSocketHelper
	{
		public static async Task<Frame> ReadFrame(WebSocket socket, CancellationToken token)
		{
			byte[] buff = new byte[1 << 16];
			var mem = new MemoryStream();

			while (true)
			{
				var result = await socket.ReceiveAsync(new ArraySegment<byte>(buff), token);
				if (result.MessageType == WebSocketMessageType.Close)
				{
					return null;
				}

				if (result.EndOfMessage)
				{
					if (result.Count != 0)
					{
						mem.Write(buff, 0, result.Count);
					}

					mem.Position = 0;

					return HotReload.Messages.Frame.Read(mem);
				}
				else
				{
					mem.Write(buff, 0, result.Count);
				}
			}
		}

		internal static async Task SendFrame(WebSocket webSocket, Frame frame, CancellationToken ct)
		{
			var stream = new MemoryStream();
			frame.WriteTo(stream);

			await webSocket.SendAsync(new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), WebSocketMessageType.Binary, true, ct);
		}
	}
}
