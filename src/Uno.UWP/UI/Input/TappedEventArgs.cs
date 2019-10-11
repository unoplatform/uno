using Windows.Devices.Input;
using Windows.Foundation;

namespace Windows.UI.Input
{
	public partial class TappedEventArgs 
	{
		internal TappedEventArgs(PointerDeviceType type, Point position, uint tapCount)
		{
			PointerDeviceType = type;
			Position = position;
			TapCount = tapCount;
		}

		public PointerDeviceType PointerDeviceType { get; }

		public global::Windows.Foundation.Point Position { get; }

		public uint TapCount { get; }
	}
}
