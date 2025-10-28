using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.Host;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.UI.RemoteControl.Server.Processors.HotReload;

/// <summary>
/// Server processor responsible to:
/// 1. Monitor file system to detect new or updated assets to notify the client,
/// 2. Serve new assets to the client upon request.
/// </summary>
internal class HotAssetsServerProcessor : IServerProcessor
{
	/// <inheritdoc />
	public string Scope => WellKnownScopes.HotAssets;

	/// <inheritdoc />
	public Task ProcessFrame(Frame frame)
		=> throw new NotImplementedException();

	/// <inheritdoc />
	public Task ProcessIdeMessage(IdeMessage message, CancellationToken ct)
		=> throw new NotImplementedException();

	/// <inheritdoc />
	public void Dispose()
		=> throw new NotImplementedException();
}

/// <summary>
/// Message sent by the client on startup to configure the hot assets monitoring on the server side.
/// </summary>
/// <param name="AssetsPaths">Paths of all assets that was packed in the app at build time.</param>
internal record ConfigureHotAssets(string[] AssetsPaths) : IMessage
{
	string IMessage.Scope => WellKnownScopes.HotAssets;
	string IMessage.Name => GetType().Name;
}
