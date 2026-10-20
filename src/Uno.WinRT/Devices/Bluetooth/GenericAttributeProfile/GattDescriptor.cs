#nullable enable

using System;

namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	public partial class GattDescriptor
	{
		internal GattDescriptor()
		{
		}

		public Guid Uuid { get; internal set; }
	}
}
