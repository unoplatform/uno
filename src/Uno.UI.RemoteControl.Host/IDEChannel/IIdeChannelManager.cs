using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.Host.IdeChannel;

#pragma warning disable IDE0055 // This tiny contract is linked into multiple projects and currently formats inconsistently.
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
#pragma warning restore IDE0055
