using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Uno.UI.RemoteControl.Server.Helpers;

namespace Uno.UI.RemoteControl.Server.Telemetry
{
	/// <summary>
	/// This class is used to let the IoC container resolve the <T> automatically 
	/// </summary>
	internal record TelemetryAdapter<T> : ITelemetry<T>
	{
		public TelemetryAdapter(TelemetrySession session)
		{
			Inner = ServiceCollectionExtensions.CreateTelemetry(typeof(T).Assembly, session);
		}

		private ITelemetry Inner { get; }

		/// <inheritdoc />
		public void Dispose()
			=> Inner.Dispose();

		/// <inheritdoc />
		public void Flush()
			=> Inner.Flush();

		/// <inheritdoc />
		public Task FlushAsync(CancellationToken ct)
			=> Inner.FlushAsync(ct) ?? Task.CompletedTask;

		/// <inheritdoc />
		public void ThreadBlockingTrackEvent(string eventName, IDictionary<string, string> properties, IDictionary<string, double> measurements)
			=> Inner.ThreadBlockingTrackEvent(eventName, properties, measurements);

		/// <inheritdoc />
		public void TrackEvent(string eventName, (string key, string value)[]? properties, (string key, double value)[]? measurements)
			=> Inner.TrackEvent(eventName, properties, measurements);

		/// <inheritdoc />
		public void TrackEvent(string eventName, IDictionary<string, string>? properties, IDictionary<string, double>? measurements)
			=> Inner.TrackEvent(eventName, properties, measurements);

		/// <inheritdoc />
		public bool Enabled => Inner.Enabled;
	}
}
