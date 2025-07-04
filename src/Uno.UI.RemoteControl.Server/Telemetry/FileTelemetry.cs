using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.Server.Telemetry
{
	/// <summary>
	/// A telemetry implementation that writes events to a file for testing purposes.
	/// Creates contextual file names based on the telemetry context (global vs connection).
	/// </summary>
	public class FileTelemetry : ITelemetry
	{
		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			WriteIndented = true
		};

		private readonly string _filePath;
		private readonly object _lock = new();

		/// <summary>
		/// Creates a FileTelemetry instance with a fixed file path.
		/// </summary>
		/// <param name="filePath">The file path to write telemetry events to</param>
		public FileTelemetry(string filePath)
		{
			_filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
			EnsureDirectoryExists(_filePath);
		}

		/// <summary>
		/// Creates a FileTelemetry instance with a contextual file name.
		/// </summary>
		/// <param name="baseFilePath">The base file path (without extension)</param>
		/// <param name="context">The telemetry context (e.g., "global", "connection", session ID)</param>
		public FileTelemetry(string baseFilePath, string context)
		{
			if (string.IsNullOrEmpty(baseFilePath))
				throw new ArgumentNullException(nameof(baseFilePath));
			if (string.IsNullOrEmpty(context))
				throw new ArgumentNullException(nameof(context));

			// Generate contextual file name: base_context_timestamp.json
			var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss", DateTimeFormatInfo.InvariantInfo);
			var directory = Path.GetDirectoryName(baseFilePath) ?? "";
			var fileNameWithoutExt = Path.GetFileNameWithoutExtension(baseFilePath);
			var extension = Path.GetExtension(baseFilePath);

			// If no extension provided, default to .json
			if (string.IsNullOrEmpty(extension))
				extension = ".json";

			var contextualFileName = $"{fileNameWithoutExt}_{context}_{timestamp}{extension}";
			_filePath = Path.Combine(directory, contextualFileName);

			EnsureDirectoryExists(_filePath);
		}

		private static void EnsureDirectoryExists(string filePath)
		{
			var directory = Path.GetDirectoryName(filePath);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
		}

		public bool Enabled => true;

		public void Dispose()
		{
			// Dont't dispose to allow post-shutdown logging
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
