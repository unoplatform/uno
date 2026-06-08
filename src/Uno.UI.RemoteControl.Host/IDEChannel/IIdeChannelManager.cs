using System;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.Host.IdeChannel;

/// <summary>
/// Controls the currently active IDE channel for the running host instance.
/// </summary>
internal interface IIdeChannelManager
{
	string? ChannelId { get; }

	bool IsConnected { get; }

	/// <summary>
	/// Raised every time a new IDE client connects to the channel and
	/// JsonRpc is attached. Subscribers should use this to publish the
	/// current environment state snapshot to the newly connected client.
	/// </summary>
	event Action? ClientConnected;

	/// <summary>
	/// Raised when a previously connected IDE client disconnects (pipe broken,
	/// JsonRpc disposed, or explicit disconnect). Subscribers can use this to
	/// start a grace-period countdown for self-termination.
	/// </summary>
	event Action? ClientDisconnected;

	/// <summary>
	/// Atomically rebinds the IDE channel to the given <paramref name="channelId"/>.
	/// The pipe lifetime is owned by the session, not the caller.
	/// </summary>
	Task<bool> RebindAsync(string? channelId);
}
