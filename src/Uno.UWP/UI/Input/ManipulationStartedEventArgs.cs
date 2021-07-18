using Windows.Devices.Input;
using Windows.Foundation;

namespace Windows.UI.Input
{
	public partial class ManipulationStartedEventArgs 
	{
		internal ManipulationStartedEventArgs(PointerDeviceType pointerDeviceType, Point position, ManipulationDelta cumulative, uint contactCount)
		{
			PointerDeviceType = pointerDeviceType;
			Position = position;
			Cumulative = cumulative;
			ContactCount = contactCount;
		}

		public PointerDeviceType PointerDeviceType { get; }
		public Point Position { get; }
		public ManipulationDelta Cumulative { get; }
		public uint ContactCount { get; }
	}
}
