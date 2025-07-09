using System.IO.Pipes;
using System.Text.Json;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers
{
	/// <summary>
	/// Minimal IDE channel client for integration tests, only for sending messages to the DevServer via named pipe.
	/// </summary>
	public class TestIdeChannelClient
	{
		private readonly string _pipeName;

		public TestIdeChannelClient(Guid pipeGuid)
		{
			_pipeName = pipeGuid.ToString();
		}

		public async Task SendToDevServerAsync(IdeMessage message, CancellationToken ct)
		{
			using var pipe = new NamedPipeClientStream(
				".",
				_pipeName,
				PipeDirection.Out,
				PipeOptions.Asynchronous | PipeOptions.WriteThrough);

			await pipe.ConnectAsync(ct);

			// Serialize the message as JSON (adapt if binary protocol is used)
			var json = JsonSerializer.Serialize(message, message.GetType());
			var bytes = System.Text.Encoding.UTF8.GetBytes(json + "\n");
			await pipe.WriteAsync(bytes, 0, bytes.Length, ct);
			await pipe.FlushAsync(ct);
		}
	}
}
