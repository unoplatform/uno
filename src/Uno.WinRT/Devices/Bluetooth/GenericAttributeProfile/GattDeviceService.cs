#nullable enable

using System;

namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	public partial class GattDeviceService : IDisposable
	{
		internal GattDeviceService()
		{
		}

		public Guid Uuid { get; internal set; }
	}
}
