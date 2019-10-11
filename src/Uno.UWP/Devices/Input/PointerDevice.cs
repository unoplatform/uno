using System.Runtime.Serialization.Formatters;

namespace Windows.Devices.Input
{
	public partial class PointerDevice
	{
		private static readonly PointerDevice _touch = new PointerDevice(PointerDeviceType.Touch);
		private static readonly PointerDevice _mouse = new PointerDevice(PointerDeviceType.Mouse);
		private static readonly PointerDevice _pen = new PointerDevice(PointerDeviceType.Pen);

		internal static PointerDevice For(PointerDeviceType type)
		{
			// We cache them as we don't implement any other properties than the PointerDeviceType
			// but this is probably not really valid...
			switch (type)
			{
				case PointerDeviceType.Touch: return _touch;
				case PointerDeviceType.Mouse: return _mouse;
				case PointerDeviceType.Pen: return _pen;
				default: return new PointerDevice(type);
			}
		}

		public PointerDevice(PointerDeviceType type)
		{
			PointerDeviceType = type;
		}

		public PointerDeviceType PointerDeviceType { get; }
	}
}
