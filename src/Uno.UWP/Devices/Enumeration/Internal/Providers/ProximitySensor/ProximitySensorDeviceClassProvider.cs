#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Uno.Devices.Enumeration.Internal;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Enumeration.Internal.Providers.ProximitySensor;

internal class ProximitySensorDeviceClassProvider : IDeviceClassProvider
{
	public bool CanWatch => false;

#pragma warning disable CS0067
	public event EventHandler<DeviceInformation>? WatchAdded;

	public event EventHandler<DeviceInformation>? WatchEnumerationCompleted;

	public event EventHandler<DeviceInformationUpdate>? WatchRemoved;

	public event EventHandler<object>? WatchStopped;

	public event EventHandler<DeviceInformationUpdate>? WatchUpdated;
#pragma warning restore CS0067

	public Task<DeviceInformation[]> FindAllAsync()
	{
		var sensorManager = SensorHelpers.GetSensorManager();
		var sensors = sensorManager.GetDynamicSensorList(Android.Hardware.SensorType.Proximity);

		if (sensors is null)
		{
			return Task.FromResult(Array.Empty<DeviceInformation>());
		}

		List<DeviceInformation> devices = new();
		foreach (var sensor in sensors)
		{
			var deviceIdentifier = new DeviceIdentifier(sensor.Id.ToString(CultureInfo.InvariantCulture), DeviceClassGuids.ProximitySensor);
			var deviceInformation = new DeviceInformation(deviceIdentifier);
			devices.Add(deviceInformation);
		}

		return Task.FromResult(devices.ToArray());
	}

	public void WatchStart() { }

	public void WatchStop() { }
}
