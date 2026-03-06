using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.Server.Helpers;

namespace Uno.UI.RemoteControl.Server.Telemetry
{
	/// <summary>
	/// This class is used to let the IoC container resolve the <T> automatically 
	/// </summary>
	internal record TelemetryGenericAdapter<T> : ITelemetry<T>
	{
		public TelemetryGenericAdapter(TelemetrySession session)
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
		public void TrackException(Exception exception, (string key, string value)[]? properties = null, (string key, double value)[]? measurements = null)
			=> Inner.TrackException(exception, properties, measurements);

		/// <inheritdoc />
		public void TrackException(Exception exception, IDictionary<string, string>? properties = null, IDictionary<string, double>? measurements = null)
			=> Inner.TrackException(exception, properties, measurements);

		/// <inheritdoc />
		public bool Enabled => Inner.Enabled;
	}
}
