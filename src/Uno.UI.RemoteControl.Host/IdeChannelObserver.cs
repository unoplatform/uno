using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI.RemoteControl.Host.IdeChannel;
using Uno.UI.RemoteControl.Server.Telemetry;

namespace Uno.UI.RemoteControl.Host;

/// <summary>
/// Monitors the IDE channel connection state. When the IDE disconnects and no
/// reconnection occurs within the grace period, initiates a graceful shutdown.
/// This provides an alternative to <see cref="ParentProcessObserver"/> that
/// doesn't require knowing the parent PID — the IDE channel itself is the
/// liveness signal.
/// </summary>
internal static class IdeChannelObserver
{
	/// <summary>
	/// Grace period after IDE disconnection before initiating shutdown.
	/// Gives the IDE time to reconnect (e.g., during extension host restart).
	/// </summary>
	internal static TimeSpan GracePeriod { get; set; } = TimeSpan.FromSeconds(30);

	/// <summary>
	/// Starts observing the IDE channel for disconnection. Only activates when
	/// <paramref name="ppid"/> is 0 (no parent process monitoring) AND an IDE
	/// channel was configured. When both <c>ppid</c> and IDE channel are present,
	/// <see cref="ParentProcessObserver"/> is sufficient.
	/// </summary>
	internal static void Observe(
		int ppid,
		string? ideChannelId,
		IIdeChannelManager ideChannelManager,
		Action gracefulShutdown,
		ITelemetry? telemetry,
		CancellationToken ct)
	{
		// Only activate when there's no ppid-based monitoring AND we have an IDE channel
		if (ppid != 0 || string.IsNullOrWhiteSpace(ideChannelId))
		{
			return;
		}

		var log = typeof(IdeChannelObserver).Log();
		log.LogInformation(
			"IDE channel observer active — will shut down if IDE channel disconnects for >{GracePeriodSeconds}s.",
			(int)GracePeriod.TotalSeconds);

		CancellationTokenSource? graceCts = null;

		ideChannelManager.ClientDisconnected += () =>
		{
			if (ct.IsCancellationRequested)
			{
				return;
			}

			log.LogWarning("IDE channel disconnected. Starting {GracePeriodSeconds}s grace period before shutdown.",
				(int)GracePeriod.TotalSeconds);

			graceCts?.Cancel();
			graceCts?.Dispose();
			graceCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			var localCts = graceCts;

			_ = Task.Run(async () =>
			{
				try
				{
					await Task.Delay(GracePeriod, localCts.Token);
				}
				catch (OperationCanceledException)
				{
					// Grace period cancelled because IDE reconnected or host is shutting down
					return;
				}

				if (ideChannelManager.IsConnected)
				{
					log.LogInformation("IDE reconnected during grace period — shutdown cancelled.");
					return;
				}

				log.LogWarning("IDE channel did not reconnect within grace period. Initiating graceful shutdown.");
				telemetry?.TrackEvent(
					"ide-channel-lost",
					default(Dictionary<string, string>),
					null);

				gracefulShutdown();

				// Use CancellationToken.None for the forced-exit delay: gracefulShutdown()
				// cancels `ct`, so using ct here would skip the delay entirely.
				await Task.Delay(5_500, CancellationToken.None);

				telemetry?.TrackEvent(
					"ide-channel-lost-forced-exit",
					default(Dictionary<string, string>),
					null);
				await Task.Delay(250, CancellationToken.None); // Give time for analytics
				Environment.Exit(5);
			}, CancellationToken.None);
		};

		ideChannelManager.ClientConnected += () =>
		{
			if (graceCts is not null)
			{
				log.LogInformation("IDE reconnected — cancelling shutdown grace period.");
				graceCts.Cancel();
				graceCts.Dispose();
				graceCts = null;
			}
		};
	}
}
