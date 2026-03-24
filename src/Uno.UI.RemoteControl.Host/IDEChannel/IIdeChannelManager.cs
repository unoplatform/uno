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
	/// Atomically rebinds the IDE channel to the given <paramref name="channelId"/>.
	/// The pipe lifetime is owned by the session, not the caller.
	/// </summary>
	Task<bool> RebindAsync(string? channelId);
}
