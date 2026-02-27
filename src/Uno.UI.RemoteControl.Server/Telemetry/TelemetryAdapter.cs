using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uno.DevTools.Telemetry;

namespace Uno.UI.RemoteControl.Server.Telemetry
{
	/// <summary>
	/// Wrapper over Uno.Devtools.Telemetry for the local interface
	/// </summary>
	internal class TelemetryAdapter(Uno.DevTools.Telemetry.Telemetry inner) : ITelemetry
	{
		public bool Enabled => inner.Enabled;

		public void Dispose()
		{
			inner.Flush();
			inner.Dispose();
		}

		public void Flush() => inner.Flush();

		public Task FlushAsync(CancellationToken ct) => inner.FlushAsync(ct);

		public void ThreadBlockingTrackEvent(string eventName, IDictionary<string, string> properties, IDictionary<string, double> measurements)
			=> inner.ThreadBlockingTrackEvent(eventName, properties, measurements);

		public void TrackEvent(string eventName, (string key, string value)[]? properties, (string key, double value)[]? measurements)
			=> inner.TrackEvent(eventName, properties, measurements);

		public void TrackEvent(string eventName, IDictionary<string, string>? properties, IDictionary<string, double>? measurements)
			=> inner.TrackEvent(eventName, properties, measurements);

		public void TrackException(Exception exception, (string key, string value)[]? properties = null, (string key, double value)[]? measurements = null)
		{
			var propertiesDict = properties != null ? ConvertToReadOnlyDictionary(properties) : null;
			var measurementsDict = measurements != null ? ConvertToReadOnlyDictionary(measurements) : null;
			inner.TrackException(exception, propertiesDict, measurementsDict);
		}

		public void TrackException(Exception exception, IDictionary<string, string>? properties = null, IDictionary<string, double>? measurements = null)
		{
			// Convert IDictionary to IReadOnlyDictionary since the inner API requires it
			IReadOnlyDictionary<string, string>? propertiesDict = properties != null
				? new Dictionary<string, string>(properties)
				: null;
			IReadOnlyDictionary<string, double>? measurementsDict = measurements != null
				? new Dictionary<string, double>(measurements)
				: null;
			inner.TrackException(exception, propertiesDict, measurementsDict);
		}

		private static IReadOnlyDictionary<string, string> ConvertToReadOnlyDictionary((string key, string value)[] items)
		{
			var dict = new Dictionary<string, string>();
			foreach (var (key, value) in items)
			{
				dict[key] = value;
			}
			return dict;
		}

		private static IReadOnlyDictionary<string, double> ConvertToReadOnlyDictionary((string key, double value)[] items)
		{
			var dict = new Dictionary<string, double>();
			foreach (var (key, value) in items)
			{
				dict[key] = value;
			}
			return dict;
		}
	}
}
