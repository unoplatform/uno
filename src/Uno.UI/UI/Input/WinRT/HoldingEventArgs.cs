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
		internal HoldingEventArgs(uint pointerId, PointerDeviceType type, Point position, Point rawPosition, HoldingState state)
		{
			PointerId = pointerId;
			PointerDeviceType = type;
			Position = position;
			RawPosition = rawPosition;
			HoldingState = state;
		}

		internal uint PointerId { get; }

		public PointerDeviceType PointerDeviceType { get; }

		public Point Position { get; }

		/// <summary>
		/// The <see cref="PointerPoint.RawPosition"/> of the press, which is relative to the window for a UIElement recognizer.
		/// </summary>
		internal Point RawPosition { get; }

		public HoldingState HoldingState { get; }

		[NotImplemented]
		public uint ContactCount { get; } = 1;

		[NotImplemented]
		public uint CurrentContactCount => HoldingState == HoldingState.Started ? 1u : 0u;
	}
}
