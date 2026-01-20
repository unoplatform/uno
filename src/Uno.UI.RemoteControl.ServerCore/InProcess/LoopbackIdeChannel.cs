using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.Services;

namespace DevServerCore;

/// <summary>
/// Minimal IDE channel that immediately reports readiness and keeps communications in-process.
/// </summary>
public sealed class LoopbackIdeChannel : IIdeChannel
{
	public event EventHandler<IdeMessage>? MessageFromIde;

	public Task SendToIdeAsync(IdeMessage message, CancellationToken ct)
		=> Task.CompletedTask;

	public Task<bool> TrySendToIdeAsync(IdeMessage message, CancellationToken ct)
		=> Task.FromResult(false);

	public Task<bool> WaitForReady(CancellationToken ct = default)
		=> Task.FromResult(true);

	/// <summary>
	/// Emits an IDE message into the devserver pipeline.
	/// </summary>
	public void Publish(IdeMessage message)
	{
		if (message is null)
		{
			throw new ArgumentNullException(nameof(message));
		}

		MessageFromIde?.Invoke(this, message);
	}
}