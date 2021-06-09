using Windows.Devices.Input;
using Windows.Foundation;

#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	public partial class ManipulationCompletedEventArgs 
	{
		internal ManipulationCompletedEventArgs(
			PointerDeviceType pointerDeviceType,
			Point position,
			ManipulationDelta cumulative,
			ManipulationVelocities velocities,
			bool isInertial,
			uint contactCount,
			uint currentContactCount)
		{
			PointerDeviceType = pointerDeviceType;
			Position = position;
			Cumulative = cumulative;
			Velocities = velocities;
			IsInertial = isInertial;
			ContactCount = contactCount;
			CurrentContactCount = currentContactCount;
		}

		public PointerDeviceType PointerDeviceType { get; }
		public Point Position { get; }
		public ManipulationDelta Cumulative { get; }
		public ManipulationVelocities Velocities { get; }
		public uint ContactCount { get; }
		public uint CurrentContactCount { get; }
		internal bool IsInertial { get; }
	}
}
