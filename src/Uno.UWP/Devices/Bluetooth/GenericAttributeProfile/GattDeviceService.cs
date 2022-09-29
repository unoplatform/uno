using System;

namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	public partial class GattDeviceService : IDisposable
	{
		public Guid Uuid { get; internal set; }
	}
}
