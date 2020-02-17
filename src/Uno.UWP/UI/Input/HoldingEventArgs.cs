using Windows.Devices.Input;
using Windows.Foundation;

namespace Windows.UI.Input
{
	public partial class HoldingEventArgs 
	{
		internal HoldingEventArgs(uint pointerId, PointerDeviceType type, Point position, HoldingState state)
		{
			PointerId = pointerId;
			PointerDeviceType = type;
			Position = position;
			HoldingState = state;
		}

		internal uint PointerId { get; }

		public PointerDeviceType PointerDeviceType { get; }

		public Point Position { get; }

		public HoldingState HoldingState { get; }
	}
}
