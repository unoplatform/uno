using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI.RemoteControl.Server.Telemetry;

namespace Uno.UI.RemoteControl.Host;

internal class ParentProcessObserver
{
	internal static Task ObserveAsync(int ppid, Action gracefulShutdown, ITelemetry? telemetry, CancellationToken ct)
	{
		if (ppid == 0)
		{
			return Task.CompletedTask; // Nothing to monitor
		}

		var log = typeof(ParentProcessObserver).Log();

		return Task.Run(async () =>
		{
			log.LogInformation("Monitoring parent process " + ppid + " for termination.");
			while (!ct.IsCancellationRequested)
			{
				await Task.Delay(7_500, ct);

				try
				{
					Process.GetProcessById(ppid); // will throw an exception if process doesn't exists
				}
				catch (ArgumentException)
				{
					log.LogWarning($"Parent process {ppid} not found, initiating graceful shutdown...");
					telemetry?.TrackEvent(
						"parent-process-lost",
						default(Dictionary<string, string>),
						null);

					gracefulShutdown();

					// Use CancellationToken.None for the forced-exit delay: gracefulShutdown()
					// may cancel `ct`, so waiting on ct would throw and skip the forced exit.
					await Task.Delay(5_500, CancellationToken.None);
					log.LogWarning($"Graceful shutdown did not complete 5.5s after parent process {ppid} was lost, forcing exit...");
					telemetry?.TrackEvent(
						"parent-process-lost-forced-exit",
						default(Dictionary<string, string>),
						null);

					await Task.Delay(250, CancellationToken.None); // Give time for analytics to log something
					Environment.Exit(4);
				}
			}
		}, ct);
	}
}
