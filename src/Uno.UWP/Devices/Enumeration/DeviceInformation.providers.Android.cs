using System;
using System.Collections.Generic;
using Uno.Devices.Enumeration.Internal;
using Uno.Devices.Enumeration.Internal.Providers.Midi;
using Windows.Devices.Enumeration.Internal.Providers.ProximitySensor;

namespace Windows.Devices.Enumeration;

public partial class DeviceInformation
{
	private static readonly Dictionary<Guid, Func<IDeviceClassProvider>> _deviceClassProviders = new()
	{
		{ DeviceClassGuids.MidiIn, () => new MidiInDeviceClassProvider() },
		{ DeviceClassGuids.MidiOut, () => new MidiOutDeviceClassProvider() },
		{ DeviceClassGuids.ProximitySensor, () => new ProximitySensorDeviceClassProvider() },
	};
}
