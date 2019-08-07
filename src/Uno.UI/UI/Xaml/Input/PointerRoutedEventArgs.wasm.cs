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
			UIElement receiver,
			bool canBubbleNatively)
		{
			_absolutePosition = absolutePosition;
			_button = button;
			_updateKind = updateKind;

			Pointer = new Pointer(pointerId, pointerType, isInContact, isInRange: true);
			KeyModifiers = keys;
			OriginalSource = receiver; // This is not true, however we currently do not have a way to get it on WASM
			CanBubbleNatively = canBubbleNatively;
		}

		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			var frameId = _pseudoFrameId;
			var timestamp = _pseudoTimestamp;
			var device = PointerDevice.For(Pointer.PointerDeviceType);
			var position = relativeTo == null
				? _absolutePosition
				: relativeTo.TransformToVisual(null).Inverse.TransformPoint(_absolutePosition);
			var properties = GetProperties();

			return new PointerPoint(frameId, timestamp, device, Pointer.PointerId, position, Pointer.IsInContact, properties);
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
