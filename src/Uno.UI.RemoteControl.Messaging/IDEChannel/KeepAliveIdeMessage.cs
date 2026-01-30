#nullable enable

using System.Threading;

namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

public record KeepAliveIdeMessage(string Source) : IdeMessage(WellKnownScopes.IdeChannel)
{
	private static long _id;

	/// <summary>
	/// Sequence ID of the ping.
	/// </summary>
	public long SequenceId { get; init; } = Interlocked.Increment(ref _id);
}
