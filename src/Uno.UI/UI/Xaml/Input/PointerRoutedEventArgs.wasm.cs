using System;
using System.Globalization;
using System.Threading;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Input;

namespace Windows.UI.Xaml.Input
{
	partial class PointerRoutedEventArgs
	{
		private static long _pseudoNextFrameId;
		private readonly uint _pseudoFrameId = (uint)Interlocked.Increment(ref _pseudoNextFrameId);
		private readonly ulong _pseudoTimestamp = (ulong)DateTime.UtcNow.Ticks;

		private readonly Point _absolutePosition;
		private readonly VirtualKey _button;
		private readonly PointerUpdateKind _updateKind;

		internal PointerRoutedEventArgs(
			uint pointerId,
			PointerDeviceType pointerType,
			Point absolutePosition,
			bool isInContact,
			VirtualKey button,
			VirtualKeyModifiers keys,
			PointerUpdateKind updateKind,
			UIElement source,
			bool canBubbleNatively)
			: this()
		{
			_absolutePosition = absolutePosition;
			_button = button;
			_updateKind = updateKind;

			FrameId = _pseudoFrameId;
			Pointer = new Pointer(pointerId, pointerType, isInContact, isInRange: true);
			KeyModifiers = keys;
			OriginalSource = source;
			CanBubbleNatively = canBubbleNatively;
		}

		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			var timestamp = _pseudoTimestamp;
			var device = PointerDevice.For(Pointer.PointerDeviceType);
			var position = relativeTo == null
				? _absolutePosition
				: relativeTo.TransformToVisual(null).Inverse.TransformPoint(_absolutePosition);
			var properties = GetProperties();

			return new PointerPoint(FrameId, timestamp, device, Pointer.PointerId, position, position, Pointer.IsInContact, properties);
		}

		private PointerPointProperties GetProperties()
		{
			var props = new PointerPointProperties()
			{
				IsPrimary = true,
				IsInRange = Pointer.IsInRange,
				PointerUpdateKind = _updateKind
			};

			if (Pointer.IsInContact)
			{
				props.IsLeftButtonPressed = _button == VirtualKey.LeftButton;
				props.IsMiddleButtonPressed = _button == VirtualKey.MiddleButton;
				props.IsRightButtonPressed = _button == VirtualKey.RightButton;
			}

			return props;
		}
	}
}
