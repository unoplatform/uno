#nullable enable

namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	public static partial class GattProtocolError
	{
		public static byte AttributeNotFound => 10;
		public static byte AttributeNotLong => 11;
		public static byte InsufficientAuthentication => 5;
		public static byte InsufficientAuthorization => 8;
		public static byte InsufficientEncryption => 15;
		public static byte InsufficientEncryptionKeySize => 12;
		public static byte InsufficientResources => 17;
		public static byte InvalidAttributeValueLength => 13;
		public static byte InvalidHandle => 1;
		public static byte InvalidOffset => 7;
		public static byte InvalidPdu => 4;
		public static byte PrepareQueueFull => 9;
		public static byte ReadNotPermitted => 2;
		public static byte RequestNotSupported => 6;
		public static byte UnlikelyError => 14;
		public static byte UnsupportedGroupType => 15;
		public static byte WriteNotPermitted => 3;
	}
}
