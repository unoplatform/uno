using Windows.Devices.Input;
using Windows.Foundation;
using Uno;

#if IS_UNO_UI_PROJECT
namespace Microsoft.UI.Input
#else
namespace Windows.UI.Input
#endif
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

		[NotImplemented]
		public uint ContactCount { get; } = 1;

		[NotImplemented]
		public uint CurrentContactCount => HoldingState == HoldingState.Started ? 1u : 0u;
	}
}
