using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IO;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl.Helpers;

public static class WebSocketHelper
{
	const int BufferSize = 1 << 16;
	private static readonly RecyclableMemoryStreamManager manager = new RecyclableMemoryStreamManager();

	public static async Task<Frame?> ReadFrame(WebSocket socket, CancellationToken token)
	{
		var pool = ArrayPool<byte>.Shared;
		var buff = pool.Rent(BufferSize);
		var segment = new ArraySegment<byte>(buff);
		using var mem = manager.GetStream()
			?? throw new InvalidOperationException($"Unable to get memory stream");

		try
		{
			while (true)
			{
				var result = await socket.ReceiveAsync(segment, token);
				if (result.MessageType == WebSocketMessageType.Close)
				{
					return null;
				}

				if (result.EndOfMessage)
				{
					if (result.Count != 0)
					{
						await mem.WriteAsync(buff, 0, result.Count);
					}

					mem.Position = 0;

					return Frame.Read(mem);
				}
				else
				{
					await mem.WriteAsync(buff, 0, result.Count);
				}
			}
		}
		finally
		{
			pool.Return(buff);
		}
	}

	internal static async Task SendFrame(WebSocket webSocket, Frame frame, CancellationToken ct)
	{
		using var stream = manager.GetStream();
		frame.WriteTo(stream);

		await webSocket.SendAsync(new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), WebSocketMessageType.Binary, true, ct);
	}
}
