using System;

namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	public partial class GattCharacteristic
	{
		public GattCharacteristicProperties CharacteristicProperties { get; internal set; }
		public Guid Uuid { get; internal set; }
	}
}
