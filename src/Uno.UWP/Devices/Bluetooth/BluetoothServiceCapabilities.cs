using System;

namespace Windows.Devices.Bluetooth
{
	[Flags]
	public   enum BluetoothServiceCapabilities 
	{
		None = 0,
		LimitedDiscoverableMode = 1,
		PositioningService = 8,
		NetworkingService = 16,
		RenderingService = 32,
		CapturingService = 64,
		ObjectTransferService = 128,
		AudioService = 256,
		TelephoneService = 512,
		InformationService = 1024,
	}
}
