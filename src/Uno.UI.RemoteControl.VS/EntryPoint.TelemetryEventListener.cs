using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Telemetry.SessionChannel;
using Uno.UI.RemoteControl.Messaging.HotReload;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.UI.RemoteControl.VS;

public partial class EntryPoint
{
	private class TelemetryEventListener(EntryPoint ide) : ISessionChannel
	{
		/// <inheritdoc />
		public string ChannelId => "Uno platform hot-reload client application";

		/// <inheritdoc />
		public string TransportUsed => "Local_TCP";

		/// <inheritdoc />
		public ChannelProperties Properties { get; set; } = ChannelProperties.DevChannel;

		/// <inheritdoc />
		public bool IsStarted => true;

		/// <inheritdoc />
		public void Start(string sessionId) { }

		/// <inheritdoc />
		public void PostEvent(TelemetryEvent telemetryEvent)
			=> TryForward(telemetryEvent);

		/// <inheritdoc />
		public void PostEvent(TelemetryEvent telemetryEvent, IEnumerable<ITelemetryManifestRouteArgs> args)
			=> TryForward(telemetryEvent);

		private void TryForward(TelemetryEvent telemetryEvent)
		{
			if (ide is not { _ideChannelClient: { } client, _isDisposed: false, _ct: { IsCancellationRequested: false } ct })
			{
				return;
			}

			switch (telemetryEvent.Name)
			{
				case "vs/diagnostics/debugger/enccomplete":
					_ = client.SendToDevServerAsync(new HotReloadEventIdeMessage(HotReloadEvent.Completed), ct.Token);
					break;

				case "vs/diagnostics/debugger/enc/nochanges":
					_ = client.SendToDevServerAsync(new HotReloadEventIdeMessage(HotReloadEvent.NoChanges), ct.Token);
					break;

				case "vs/diagnostics/debugger/enc/error":
					_ = client.SendToDevServerAsync(new HotReloadEventIdeMessage(HotReloadEvent.Failed), ct.Token);
					break;

				case "vs/diagnostics/debugger/hotreloaddialog/buttonclick":
					_ = client.SendToDevServerAsync(new HotReloadEventIdeMessage(HotReloadEvent.RudeEditDialogButton), ct.Token);
					break;
			}
		}
	}
}
