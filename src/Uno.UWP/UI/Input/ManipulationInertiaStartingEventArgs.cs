using Windows.Devices.Input;
using Windows.Foundation;

namespace Windows.UI.Input
{
	public partial class ManipulationInertiaStartingEventArgs 
	{
		internal ManipulationInertiaStartingEventArgs(PointerDeviceType pointerDeviceType, Point position, ManipulationDelta delta, ManipulationDelta cumulative)
		{
			PointerDeviceType = pointerDeviceType;
			Position = position;
			Delta = delta;
			Cumulative = cumulative;
		}

		public PointerDeviceType PointerDeviceType { get; }
		public Point Position { get; }
		public ManipulationDelta Delta { get; }
		public ManipulationDelta Cumulative { get; }
	}
}
