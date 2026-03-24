using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.Host.IdeChannel;

/// <summary>
/// Controls the currently active IDE channel for the running host instance.
/// </summary>
internal interface IIdeChannelManager
{
	string? ChannelId { get; }

	bool IsConnected { get; }

	Task<bool> RebindAsync(
		string? channelId,
		CancellationToken cancellationToken = default);
}
