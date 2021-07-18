using Windows.Devices.Input;
using Windows.Foundation;

namespace Windows.UI.Input
{
	public partial class ManipulationInertiaStartingEventArgs 
	{
		internal ManipulationInertiaStartingEventArgs(
			PointerDeviceType pointerDeviceType,
			Point position,
			ManipulationDelta delta,
			ManipulationDelta cumulative,
			ManipulationVelocities velocities,
			uint contactCount,
			GestureRecognizer.Manipulation.InertiaProcessor processor)
		{
			PointerDeviceType = pointerDeviceType;
			Position = position;
			Delta = delta;
			Cumulative = cumulative;
			Velocities = velocities;
			ContactCount = contactCount;
			Processor = processor;
		}

		public PointerDeviceType PointerDeviceType { get; }
		public Point Position { get; }
		public ManipulationDelta Delta { get; }
		public ManipulationDelta Cumulative { get; }
		public ManipulationVelocities Velocities { get; }
		public uint ContactCount { get; }

		internal GestureRecognizer.Manipulation.InertiaProcessor Processor { get; }
	}
}
