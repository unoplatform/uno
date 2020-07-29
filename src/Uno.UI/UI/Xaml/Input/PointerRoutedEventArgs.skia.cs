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
		private static long _pseudoNextFrameId;
		private readonly uint _pseudoFrameId = (uint)Interlocked.Increment(ref _pseudoNextFrameId);
		private readonly ulong _pseudoTimestamp = (ulong)DateTime.UtcNow.Ticks;

		internal PointerRoutedEventArgs(
			PointerEventArgs pointerEventArgs,
			Pointer pointer,
			UIElement source) : this()
		{
			_pointerEventArgs = pointerEventArgs;
			_absolutePosition = pointerEventArgs.CurrentPoint.RawPosition;

			FrameId = _pseudoFrameId;
			Pointer = pointer;
			OriginalSource = source;
		}

		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			var device = PointerDevice.For(PointerDeviceType.Mouse);
			var position = relativeTo == null
				? _absolutePosition
				: relativeTo.TransformToVisual(null).Inverse.TransformPoint(_absolutePosition);

			var properties = _pointerEventArgs.CurrentPoint.Properties;

			return new PointerPoint(FrameId, _pseudoTimestamp, device, 0, _absolutePosition, position, true, properties);
		}
	}
}
