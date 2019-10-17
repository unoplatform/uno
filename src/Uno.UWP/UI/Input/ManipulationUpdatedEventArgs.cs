using Windows.Devices.Input;
using Windows.Foundation;

namespace Windows.UI.Input
{
	public  partial class ManipulationUpdatedEventArgs 
	{
		internal ManipulationUpdatedEventArgs(
			PointerDeviceType pointerDeviceType,
			Point position,
			ManipulationDelta delta,
			ManipulationDelta cumulative,
			bool isInertial)
		{
			PointerDeviceType = pointerDeviceType;
			Position = position;
			Delta = delta;
			Cumulative = cumulative;
			IsInertial = isInertial;
		}

		public PointerDeviceType PointerDeviceType { get; }
		public Point Position { get; }
		public ManipulationDelta Delta { get; }
		public ManipulationDelta Cumulative { get; }

		internal bool IsInertial { get; }
	}
}
