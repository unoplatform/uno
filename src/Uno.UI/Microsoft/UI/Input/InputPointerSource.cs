#if HAS_UNO_WINUI

#pragma warning disable 67

namespace Microsoft.UI.Input
{
	public sealed partial class InputPointerSource : InputObject
	{
		public InputCursor Cursor { get; set; }

		public InputPointerSourceDeviceKinds DeviceKinds
			=> InputPointerSourceDeviceKinds.Mouse | InputPointerSourceDeviceKinds.Pen | InputPointerSourceDeviceKinds.Touch;

		public event Windows.Foundation.TypedEventHandler<InputPointerSource, PointerEventArgs> PointerCaptureLost;

		public event Windows.Foundation.TypedEventHandler<InputPointerSource, PointerEventArgs> PointerEntered;

		public event Windows.Foundation.TypedEventHandler<InputPointerSource, PointerEventArgs> PointerExited;

		public event Windows.Foundation.TypedEventHandler<InputPointerSource, PointerEventArgs> PointerMoved;

		public event Windows.Foundation.TypedEventHandler<InputPointerSource, PointerEventArgs> PointerPressed;

		public event Windows.Foundation.TypedEventHandler<InputPointerSource, PointerEventArgs> PointerReleased;

		public event Windows.Foundation.TypedEventHandler<InputPointerSource, PointerEventArgs> PointerRoutedAway;

		public event Windows.Foundation.TypedEventHandler<InputPointerSource, PointerEventArgs> PointerRoutedReleased;

		public event Windows.Foundation.TypedEventHandler<InputPointerSource, PointerEventArgs> PointerRoutedTo;

		public event Windows.Foundation.TypedEventHandler<InputPointerSource, PointerEventArgs> PointerWheelChanged;
	}
}
#endif
