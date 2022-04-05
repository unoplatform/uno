using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	public partial class GattDeviceServicesResult
	{
		public  byte? ProtocolError { get; internal set; }
		public  IReadOnlyList<GattDeviceService> Services { get; internal set; }
		public  GattCommunicationStatus Status { get; internal set; }
	}

}
