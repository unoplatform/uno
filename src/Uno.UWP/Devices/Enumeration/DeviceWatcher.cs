using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Devices.Enumeration.Internal;

namespace Windows.Devices.Enumeration
{
	public partial class DeviceWatcher
	{
		private IDeviceClassProvider[] _providers;

		internal DeviceWatcher(IDeviceClassProvider[] providers)
		{
			_providers = providers;
		}
	}
}
