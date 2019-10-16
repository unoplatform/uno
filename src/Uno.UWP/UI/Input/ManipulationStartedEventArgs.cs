using Windows.Devices.Input;
using Windows.Foundation;

namespace Windows.UI.Input
{
	public partial class ManipulationStartedEventArgs 
	{
		internal ManipulationStartedEventArgs(PointerDeviceType pointerDeviceType, Point position, ManipulationDelta cumulative)
		{
			PointerDeviceType = pointerDeviceType;
			Position = position;
			Cumulative = cumulative;
		}

		public PointerDeviceType PointerDeviceType { get; }
		public Point Position { get; }
		public ManipulationDelta Cumulative { get; }
	}
}
