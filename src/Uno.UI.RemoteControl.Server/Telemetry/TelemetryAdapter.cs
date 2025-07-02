using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.UI.RemoteControl.Server.Telemetry
{
	public record TelemetryAdapter<T>(IServiceProvider services) : ITelemetry<T>
	{
		private ITelemetry? Inner { get; } = services.GetService<ITelemetry>();

		/// <inheritdoc />
		public void Dispose()
			=> Inner?.Dispose();

		/// <inheritdoc />
		public void Flush()
			=> Inner?.Flush();

		/// <inheritdoc />
		public Task FlushAsync(CancellationToken ct)
			=> Inner?.FlushAsync(ct) ?? Task.CompletedTask;

		/// <inheritdoc />
		public void ThreadBlockingTrackEvent(string eventName, IDictionary<string, string> properties, IDictionary<string, double> measurements)
			=> Inner?.ThreadBlockingTrackEvent(eventName, properties, measurements);

		/// <inheritdoc />
		public void TrackEvent(string eventName, (string key, string value)[]? properties, (string key, double value)[]? measurements)
			=> Inner?.TrackEvent(eventName, properties, measurements);

		/// <inheritdoc />
		public void TrackEvent(string eventName, IDictionary<string, string>? properties, IDictionary<string, double>? measurements)
			=> Inner?.TrackEvent(eventName, properties, measurements);

		/// <inheritdoc />
		public bool Enabled => Inner?.Enabled ?? false;
	}
}
