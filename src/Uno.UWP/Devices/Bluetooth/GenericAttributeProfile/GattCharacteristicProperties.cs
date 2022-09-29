namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	public enum GattCharacteristicProperties : uint
	{
		None = 0,
		Broadcast = 1,
		Read = 2,
		WriteWithoutResponse = 4,
		Write = 8,
		Notify = 16,
		Indicate = 32,
		AuthenticatedSignedWrites = 64,
		ExtendedProperties = 128,
		ReliableWrites = 256,
		WritableAuxiliaries = 512,
	}
}
