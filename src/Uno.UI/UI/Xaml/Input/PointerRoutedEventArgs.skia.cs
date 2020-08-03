using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Windows.Devices.Input;
using Uno;
using Windows.Foundation;
using Windows.UI.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Core;

namespace Windows.UI.Xaml.Input
{
	partial class PointerRoutedEventArgs
	{
		private readonly PointerEventArgs _pointerEventArgs;
		private readonly Point _absolutePosition;

		internal PointerRoutedEventArgs(
			PointerEventArgs pointerEventArgs,
			UIElement source) : this()
		{
			_pointerEventArgs = pointerEventArgs;
			_absolutePosition = pointerEventArgs.CurrentPoint.RawPosition;

			FrameId = pointerEventArgs.CurrentPoint.FrameId;
			Pointer = GetPointer(pointerEventArgs);
			OriginalSource = source;

			// All events bubble in managed mode.
			CanBubbleNatively = false;
		}

		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			var device = PointerDevice.For(PointerDeviceType.Mouse);
			var position = relativeTo == null
				? _absolutePosition
				: relativeTo.TransformToVisual(null).Inverse.TransformPoint(_absolutePosition);
			var timestamp = _pointerEventArgs.CurrentPoint.Timestamp;
			var properties = _pointerEventArgs.CurrentPoint.Properties;

			return new PointerPoint(FrameId, timestamp, device, 0, _absolutePosition, position, true, properties);
		}

		private Pointer GetPointer(PointerEventArgs args)
			=> new Pointer(
				args.CurrentPoint.PointerId,
				args.CurrentPoint.PointerDevice.PointerDeviceType,
				isInContact: args.CurrentPoint.IsInContact,
				isInRange: args.CurrentPoint.Properties.IsInRange);
	}
}
