#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Android.Hardware;
using Uno.Devices.Enumeration.Internal;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Enumeration.Internal.Providers.ProximitySensor;

internal class ProximitySensorDeviceClassProvider : IDeviceClassProvider
{
	public bool CanWatch => false;

#pragma warning disable CS0067
	public event EventHandler<DeviceInformation>? WatchAdded;

	public event EventHandler<DeviceInformation?>? WatchEnumerationCompleted;

	public event EventHandler<DeviceInformationUpdate>? WatchRemoved;

	public event EventHandler<object?>? WatchStopped;

	public event EventHandler<DeviceInformationUpdate>? WatchUpdated;
#pragma warning restore CS0067

	public Task<DeviceInformation[]> FindAllAsync() => Task.Run(() =>
	{
		var sensorManager = SensorHelpers.GetSensorManager();
		var sensors = sensorManager.GetDynamicSensorList(SensorType.Proximity);

		List<DeviceInformation> devices = new();
		if (sensors is not { Count: > 0 })
		{
			sensors = sensorManager.GetSensorList(SensorType.Proximity);
		}

		if (sensors is not { Count: > 0 })
		{
			if (sensorManager.GetDefaultSensor(SensorType.Proximity) is { } sensor)
			{
				sensors = new List<Sensor>() { sensor };
			}
		}

		foreach (var sensor in sensors ?? Array.Empty<Sensor>())
		{
			var deviceIdentifier = new DeviceIdentifier(sensor.Id.ToString(CultureInfo.InvariantCulture), DeviceClassGuids.ProximitySensor);
			var deviceInformation = new DeviceInformation(deviceIdentifier);
			devices.Add(deviceInformation);
		}

		return Task.FromResult(devices.ToArray());
	});

	public void WatchStart() { }

	public void WatchStop() { }
}
