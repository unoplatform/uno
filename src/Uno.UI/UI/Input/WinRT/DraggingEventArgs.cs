// On the UWP branch, only include this file in Uno.UWP (as public Window.whatever). On the WinUI branch, include it in both Uno.UWP (internal as Windows.whatever) and Uno.UI (public as Microsoft.whatever)
#if HAS_UNO_WINUI || !IS_UNO_UI_PROJECT
using Windows.Devices.Input;
using Windows.Foundation;

#if HAS_UNO_WINUI && IS_UNO_UI_PROJECT
namespace Microsoft.UI.Input
#else
namespace Windows.UI.Input
#endif
{
	public partial class DraggingEventArgs
	{
		internal DraggingEventArgs(PointerPoint point, DraggingState state, uint contactCount)
		{
			Pointer = point;
			DraggingState = state;
			ContactCount = contactCount;
		}

		internal PointerPoint Pointer { get; }

		public DraggingState DraggingState { get; }

		public PointerDeviceType PointerDeviceType => (PointerDeviceType)Pointer.PointerDevice.PointerDeviceType;

		public Point Position => Pointer.Position;

		public uint ContactCount { get; }
	}
}
#endif
