using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.Server.Telemetry
{
	/// <summary>
	/// A telemetry implementation that writes events to a file for testing purposes.
	/// </summary>
	public class FileTelemetry : ITelemetry
	{
		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			WriteIndented = true
		};

		private readonly string _filePath;
		private readonly object _lock = new();
		private bool _disposed;

		public FileTelemetry(string filePath)
		{
			_filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));

			// Ensure the directory exists
			var directory = Path.GetDirectoryName(_filePath);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
		}

		public bool Enabled => true;

		public void Dispose()
		{
			_disposed = true;
		}

		public void Flush()
		{
			// File-based telemetry doesn't need explicit flushing as we write immediately
		}

		public Task FlushAsync(CancellationToken ct)
		{
			// File-based telemetry doesn't need explicit flushing as we write immediately
			return Task.CompletedTask;
		}

		public void ThreadBlockingTrackEvent(string eventName, IDictionary<string, string> properties, IDictionary<string, double> measurements)
		{
			TrackEvent(eventName, properties, measurements);
		}

		public void TrackEvent(string eventName, (string key, string value)[]? properties, (string key, double value)[]? measurements)
		{
			var propertiesDict = properties != null ? new Dictionary<string, string>() : null;
			if (properties != null)
			{
				foreach (var (key, value) in properties)
				{
					propertiesDict![key] = value;
				}
			}

			var measurementsDict = measurements != null ? new Dictionary<string, double>() : null;
			if (measurements != null)
			{
				foreach (var (key, value) in measurements)
				{
					measurementsDict![key] = value;
				}
			}

			TrackEvent(eventName, propertiesDict, measurementsDict);
		}

		public void TrackEvent(string eventName, IDictionary<string, string>? properties, IDictionary<string, double>? measurements)
		{
			if (_disposed)
			{
				return;
			}

			var telemetryEvent = new
			{
				Timestamp = DateTimeOffset.UtcNow,
				EventName = eventName,
				Properties = properties,
				Measurements = measurements
			};

			var json = JsonSerializer.Serialize(telemetryEvent, JsonOptions);

			lock (_lock)
			{
				try
				{
					File.AppendAllText(_filePath, json + Environment.NewLine);
				}
				catch
				{
					// Ignore file write errors in telemetry to avoid breaking the application
				}
			}
		}
	}
}
