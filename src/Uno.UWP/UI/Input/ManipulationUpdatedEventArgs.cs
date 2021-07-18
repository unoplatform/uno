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
			ManipulationVelocities velocities,
			bool isInertial,
			uint contactCount,
			uint currentContactCount)
		{
			PointerDeviceType = pointerDeviceType;
			Position = position;
			Delta = delta;
			Cumulative = cumulative;
			Velocities = velocities;
			IsInertial = isInertial;
			ContactCount = contactCount;
			CurrentContactCount = currentContactCount;
		}

		public PointerDeviceType PointerDeviceType { get; }
		public Point Position { get; }
		public ManipulationDelta Delta { get; }
		public ManipulationDelta Cumulative { get; }
		public ManipulationVelocities Velocities { get; }
		public uint ContactCount { get; }
		public uint CurrentContactCount { get; }

		internal bool IsInertial { get; }
	}
}
