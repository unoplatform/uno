// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// 2019/04/12 (Jerome Laban <jerome.laban@nventive.com>):
//	- Extracted from dotnet.exe
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.DotNet.PlatformAbstractions;

namespace Uno.UI.SourceGenerators.Telemetry
{
	public class Telemetry
	{
		internal static string CurrentSessionId = null;
		private readonly int _senderCount;
		private TelemetryClient _client = null;
		private Dictionary<string, string> _commonProperties = null;
		private Dictionary<string, double> _commonMeasurements = null;
		private Task _trackEventTask = null;

		private const string InstrumentationKey = "9a44058e-1913-4721-a979-9582ab8bedce";
		private const string TelemetryOptout = "UNO_PLATFORM_TELEMETRY_OPTOUT";

		public bool Enabled { get; }

		public Telemetry() : this(null) { }

		public Telemetry(Func<bool?> enabledProvider) : this(null, enabledProvider: enabledProvider) { }

		public Telemetry(
			string sessionId,
			bool blockThreadInitialization = false,
			Func<bool?> enabledProvider = null,
			int senderCount = 3)
		{
			if (bool.TryParse(Environment.GetEnvironmentVariable(TelemetryOptout), out var telemetryOptOut))
			{
				Enabled = !telemetryOptOut;
			}
			else
			{
				Enabled = !enabledProvider?.Invoke() ?? true;
			}

			if (!Enabled)
			{
				return;
			}

			// Store the session ID in a static field so that it can be reused
			CurrentSessionId = sessionId ?? Guid.NewGuid().ToString();
			_senderCount = senderCount;
			if (blockThreadInitialization)
			{
				InitializeTelemetry();
			}
			else
			{
				//initialize in task to offload to parallel thread
				_trackEventTask = Task.Run(() => InitializeTelemetry());
			}
		}

		public void TrackEvent(
			string eventName,
			(string key, string value)[] properties,
			(string key, double value)[] measurements)
			=> TrackEvent(eventName, properties?.ToDictionary(p => p.key, p => p.value), measurements?.ToDictionary(p => p.key, p => p.value));

		public void TrackEvent(string eventName, IDictionary<string, string> properties,
			IDictionary<string, double> measurements)
		{
			if (!Enabled)
			{
				return;
			}

			//continue the task in different threads
			_trackEventTask = _trackEventTask.ContinueWith(
				x => TrackEventTask(eventName, properties, measurements)
			);
		}

		public void Flush()
		{
			if (!Enabled || _trackEventTask == null)
			{
				return;
			}

			_trackEventTask.Wait();
		}

		public void ThreadBlockingTrackEvent(string eventName, IDictionary<string, string> properties, IDictionary<string, double> measurements)
		{
			if (!Enabled)
			{
				return;
			}
			TrackEventTask(eventName, properties, measurements);
		}

		private void InitializeTelemetry()
		{
			try
			{
				var persistenceChannel = new PersistenceChannel.PersistenceChannel(sendersCount: _senderCount);
				persistenceChannel.SendingInterval = TimeSpan.FromMilliseconds(1);
				TelemetryConfiguration.Active.TelemetryChannel = persistenceChannel;

				_commonProperties = new TelemetryCommonProperties().GetTelemetryCommonProperties();
				_commonMeasurements = new Dictionary<string, double>();

				_client = new TelemetryClient();
				_client.InstrumentationKey = InstrumentationKey;
				_client.Context.User.Id = _commonProperties[TelemetryCommonProperties.MachineId];
				_client.Context.Session.Id = CurrentSessionId;
				_client.Context.Device.OperatingSystem = RuntimeEnvironment.OperatingSystem;
			}
			catch (Exception e)
			{
				_client = null;
				// we don't want to fail the tool if telemetry fails.
				Debug.Fail(e.ToString());
			}
		}

		private void TrackEventTask(
			string eventName,
			IDictionary<string, string> properties,
			IDictionary<string, double> measurements)
		{
			if (_client == null)
			{
				return;
			}

			try
			{
				var eventProperties = GetEventProperties(properties);
				var eventMeasurements = GetEventMeasures(measurements);

				_client.TrackEvent(PrependProducerNamespace(eventName), eventProperties, eventMeasurements);
			}
			catch (Exception e)
			{
				Debug.Fail(e.ToString());
			}
		}

		private static string PrependProducerNamespace(string eventName)
		{
			return "uno/generation/" + eventName;
		}

		private Dictionary<string, double> GetEventMeasures(IDictionary<string, double> measurements)
		{
			var eventMeasurements = new Dictionary<string, double>(_commonMeasurements);
			if (measurements != null)
			{
				foreach (var measurement in measurements)
				{
					eventMeasurements[measurement.Key] = measurement.Value;
				}
			}
			return eventMeasurements;
		}

		private Dictionary<string, string> GetEventProperties(IDictionary<string, string> properties)
		{
			if (properties != null)
			{
				var eventProperties = new Dictionary<string, string>(_commonProperties);
				foreach (var property in properties)
				{
					eventProperties[property.Key] = property.Value;
				}
				return eventProperties;
			}
			else
			{
				return _commonProperties;
			}
		}
	}
}
