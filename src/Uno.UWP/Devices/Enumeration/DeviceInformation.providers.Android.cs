using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Devices.Enumeration.Internal;
using Uno.Devices.Enumeration.Internal.Providers.Midi;

namespace Windows.Devices.Enumeration
{
	public partial class DeviceInformation
	{
		private static readonly Dictionary<string, Func<IDeviceClassProvider>> _deviceClassProviders = new Dictionary<string, Func<IDeviceClassProvider>>()
		{
			{ DeviceClassGuids.MidiIn, () => new MidiInDeviceClassProvider() },
			{ DeviceClassGuids.MidiOut, () => new MidiOutDeviceClassProvider() },
		};
	}
}
