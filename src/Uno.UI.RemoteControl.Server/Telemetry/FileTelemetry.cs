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
	/// Can use either individual files per context or a single file with prefixes.
	/// </summary>
	public class FileTelemetry : ITelemetry
	{
		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			WriteIndented = false // Use single-line JSON for easier parsing in tests
		};

		private readonly string _filePath;
		private readonly string _contextPrefix;
		private readonly string? _eventsPrefix;
		private readonly object _lock = new();

		/// <summary>
		/// Creates a FileTelemetry instance with a contextual file name or prefix.
		/// </summary>
		/// <param name="baseFilePath">The base file path (with or without extension)</param>
		/// <param name="context">The telemetry context (e.g., "global", "connection", session ID)</param>
		/// <param name="eventsPrefix">The events prefix to prepend to event names (e.g., "uno/dev-server")</param>
		public FileTelemetry(string baseFilePath, string context, string? eventsPrefix = null)
		{
			if (string.IsNullOrEmpty(baseFilePath))
			{
				throw new ArgumentNullException(nameof(baseFilePath));
			}

			if (string.IsNullOrEmpty(context))
			{
				throw new ArgumentNullException(nameof(context));
			}

			_filePath = baseFilePath;
			_contextPrefix = context; // Save the context as a prefix for events
			_eventsPrefix = eventsPrefix;
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

		/// <summary>
		/// Applies the events prefix to the event name if configured.
		/// </summary>
		private string ApplyEventsPrefix(string eventName)
		{
			if (string.IsNullOrEmpty(_eventsPrefix))
			{
				return eventName;
			}

			return $"{_eventsPrefix}/{eventName}";
		}

		public bool Enabled => true;

		public void Dispose()
		{
			// Don't dispose to allow post-shutdown logging
		}

		public void Flush()
		{
			// File-based telemetry doesn't need explicit flushing as we write immediately
		}

		public Task FlushAsync(CancellationToken ct) => Task.CompletedTask;

		public void ThreadBlockingTrackEvent(string eventName, IDictionary<string, string> properties,
			IDictionary<string, double> measurements)
		{
			TrackEvent(eventName, properties, measurements);
		}

		public void TrackEvent(string eventName, (string key, string value)[]? properties,
			(string key, double value)[]? measurements)
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

		public void TrackEvent(string eventName, IDictionary<string, string>? properties,
			IDictionary<string, double>? measurements)
		{
			// Apply events prefix to event name
			var prefixedEventName = ApplyEventsPrefix(eventName);

			// Add the context prefix to properties if specified
			var finalProperties = properties;
			if (!string.IsNullOrEmpty(_contextPrefix))
			{
				// Clone properties and add context
				finalProperties = properties != null
					? new Dictionary<string, string>(properties)
					: [];
			}

			var telemetryEvent = new
			{
				Timestamp = DateTime.Now, // Use local time for easier follow-up
				EventName = prefixedEventName,
				Properties = finalProperties,
				Measurements = measurements
			};

			var json = JsonSerializer.Serialize(telemetryEvent, JsonOptions);

			lock (_lock)
			{
				try
				{
					var line = _contextPrefix + ": " + json + Environment.NewLine;
					File.AppendAllText(_filePath, line);
				}
				catch
				{
					// Ignore file write errors in telemetry to avoid breaking the application
				}
			}
		}

		public void TrackException(Exception exception, (string key, string value)[]? properties = null, (string key, double value)[]? measurements = null)
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

			TrackException(exception, propertiesDict, measurementsDict);
		}

		public void TrackException(Exception exception, IDictionary<string, string>? properties = null, IDictionary<string, double>? measurements = null)
		{
			// For file-based telemetry, write exception details as a special event
			var exceptionProperties = properties != null
				? new Dictionary<string, string>(properties)
				: new Dictionary<string, string>();

			exceptionProperties["ExceptionType"] = exception.GetType().FullName ?? exception.GetType().Name;
			exceptionProperties["ExceptionMessage"] = exception.Message;
			if (exception.StackTrace != null)
			{
				exceptionProperties["ExceptionStackTrace"] = exception.StackTrace;
			}

			var telemetryEvent = new
			{
				Timestamp = DateTime.Now,
				EventType = "Exception",
				Properties = exceptionProperties,
				Measurements = measurements
			};

			var json = JsonSerializer.Serialize(telemetryEvent, JsonOptions);

			lock (_lock)
			{
				try
				{
					var line = _contextPrefix + ": " + json + Environment.NewLine;
					File.AppendAllText(_filePath, line);
				}
				catch
				{
					// Ignore file write errors in telemetry to avoid breaking the application
				}
			}
		}
	}
}
