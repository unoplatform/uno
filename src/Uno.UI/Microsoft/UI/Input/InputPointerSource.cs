#if HAS_UNO_WINUI

using Windows.Foundation;

#pragma warning disable 67

namespace Microsoft.UI.Input
{
	public sealed partial class InputPointerSource : InputObject
	{
		public InputCursor Cursor { get; set; }

		public InputPointerSourceDeviceKinds DeviceKinds
			=> InputPointerSourceDeviceKinds.Mouse | InputPointerSourceDeviceKinds.Pen | InputPointerSourceDeviceKinds.Touch;

		public event TypedEventHandler<InputPointerSource, PointerEventArgs> PointerCaptureLost;

		public event TypedEventHandler<InputPointerSource, PointerEventArgs> PointerEntered;

		public event TypedEventHandler<InputPointerSource, PointerEventArgs> PointerExited;

		public event TypedEventHandler<InputPointerSource, PointerEventArgs> PointerMoved;

		public event TypedEventHandler<InputPointerSource, PointerEventArgs> PointerPressed;

		public event TypedEventHandler<InputPointerSource, PointerEventArgs> PointerReleased;

		public event TypedEventHandler<InputPointerSource, PointerEventArgs> PointerRoutedAway;

		public event TypedEventHandler<InputPointerSource, PointerEventArgs> PointerRoutedReleased;

		public event TypedEventHandler<InputPointerSource, PointerEventArgs> PointerRoutedTo;

		public event TypedEventHandler<InputPointerSource, PointerEventArgs> PointerWheelChanged;
	}
}
#endif
