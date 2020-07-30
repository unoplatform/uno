using System;
using System.Collections.Generic;
using Uno.Devices.Enumeration.Internal;
using Uno.Devices.Enumeration.Internal.Providers.Midi;

namespace Windows.Devices.Enumeration
{
	public partial class DeviceInformation
	{
		private static readonly Dictionary<Guid, Func<IDeviceClassProvider>> _deviceClassProviders = new Dictionary<Guid, Func<IDeviceClassProvider>>()
		{
			{ DeviceClassGuids.MidiIn, () => new MidiInDeviceClassProvider() },
			{ DeviceClassGuids.MidiOut, () => new MidiOutDeviceClassProvider() },
		};
	}
}
