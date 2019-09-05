using System;
using System.Globalization;
using System.Threading;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Input;
using Uno;
using Uno.Extensions;
using Uno.Foundation;

namespace Windows.UI.Xaml.Input
{
	partial class PointerRoutedEventArgs
	{
		private readonly ulong _timestamp;
		private readonly Point _absolutePosition;
		private readonly VirtualKey _button;
		private readonly PointerUpdateKind _updateKind;

		internal PointerRoutedEventArgs(
			ulong timestamp,
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
			_timestamp = timestamp;
			_absolutePosition = absolutePosition;
			_button = button;
			_updateKind = updateKind;

			FrameId = ToFrameId(timestamp);
			Pointer = new Pointer(pointerId, pointerType, isInContact, isInRange: true);
			KeyModifiers = keys;
			OriginalSource = source;
			CanBubbleNatively = canBubbleNatively;
		}

		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			var timestamp = ToTimeStamp(_timestamp);
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

		#region Misc static helpers
		private static ulong? _bootTime;

		private static ulong ToTimeStamp(ulong timestamp)
		{
			if (!_bootTime.HasValue)
			{
				_bootTime = ulong.Parse(WebAssemblyRuntime.InvokeJS("Date.now() - performance.now()")) * TimeSpan.TicksPerMillisecond;
			}

			return _bootTime.Value + (timestamp * TimeSpan.TicksPerMillisecond);
		}

		private static uint ToFrameId(ulong timestamp)
		{
			// Known limitation: After 49 days, we will overflow the uint and frame IDs will restart at 0.
			return (uint)(timestamp % uint.MaxValue);
		}
		#endregion
	}
}
