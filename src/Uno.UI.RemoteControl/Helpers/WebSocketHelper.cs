using System;
using System.Buffers;
using System.IO;
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

	/// <summary>
	/// Reads a full frame from the websocket, returning null if the socket was closed.
	/// </summary>
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

				if (result.Count != 0)
				{
					await mem.WriteAsync(buff, 0, result.Count, token);
				}

				if (result.EndOfMessage)
				{
					mem.Position = 0;

					try
					{
						return Frame.Read(mem);
					}
					catch (Exception error)
					{
#if IS_DEVSERVER
						var log = Uno.Extensions.LogExtensionPoint.Log(typeof(Frame));
						if (log.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
						{
							Microsoft.Extensions.Logging.LoggerExtensions.LogError(log, error, "Failed to read frame");
						}
#else // Client
						var log = Uno.Foundation.Logging.LogExtensionPoint.Log(typeof(Frame));
						if (log.IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
						{
							log.LogError("Failed to read frame", error);
						}
#endif

						mem.Position = 0;
						mem.SetLength(0);
					}
				}
			}
		}
		catch (IOException ex)
		{
#if IS_DEVSERVER
			var log = Uno.Extensions.LogExtensionPoint.Log(typeof(Frame));
			if (log.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				ex.ToString();
				Microsoft.Extensions.Logging.LoggerExtensions.LogDebug(log, "Connection reset by peer.");
			}
#else // Client
			var log = Uno.Foundation.Logging.LogExtensionPoint.Log(typeof(Frame));
			if (log.IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				log.LogDebug("Connection reset by peer.", ex);
			}
#endif
			return null; // Connection reset by peer, no need to report this.
		}
		catch (WebSocketException ex)
		{
#if IS_DEVSERVER
			var log = Uno.Extensions.LogExtensionPoint.Log(typeof(Frame));
			if (log.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
			{
				ex.ToString();
				Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(log, "WebSocket connection closed.");
			}
#else // Client
			var log = Uno.Foundation.Logging.LogExtensionPoint.Log(typeof(Frame));
			if (log.IsEnabled(Uno.Foundation.Logging.LogLevel.Information))
			{
				log.LogInfo("WebSocket connection closed.", ex);
			}
#endif
			return null; // WebSocket closed, no need to report this as an error.
		catch (Exception ex) when (ex is IOException or WebSocketException)
		{
#if IS_DEVSERVER
			var log = Uno.Extensions.LogExtensionPoint.Log(typeof(Frame));
			if (log.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
			{
				Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(log, "Connection reset by peer.");
			}
#else // Client
			var log = Uno.Foundation.Logging.LogExtensionPoint.Log(typeof(Frame));
			if (log.IsEnabled(Uno.Foundation.Logging.LogLevel.Information))
			{
				log.LogInformation("Connection reset by peer.", ex);
			}
#endif
			return null; // Connection reset by peer, no need to report this.
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

		await webSocket.SendAsync(new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Position), WebSocketMessageType.Binary, true, ct);
	}
}
