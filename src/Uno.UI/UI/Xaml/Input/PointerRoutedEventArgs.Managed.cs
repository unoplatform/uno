#if UNO_HAS_MANAGED_POINTERS
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Windows.Devices.Input;
using Uno;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Windows.UI.Core;

using PointerEventArgs = Windows.UI.Core.PointerEventArgs;
using Uno.Extensions;


#if HAS_UNO_WINUI
using Microsoft.UI.Input;
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;
#else
using Windows.UI.Input;
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

namespace Windows.UI.Xaml.Input
{
	partial class PointerRoutedEventArgs
	{
		private readonly PointerEventArgs _pointerEventArgs;
		private readonly PointerPoint _currentPoint;

		internal PointerEventArgs CoreArgs => _pointerEventArgs;

		internal PointerRoutedEventArgs(
			PointerEventArgs pointerEventArgs,
			UIElement source) : this()
		{
			_pointerEventArgs = pointerEventArgs;

#if HAS_UNO_WINUI
			_currentPoint = new PointerPoint(_pointerEventArgs.CurrentPoint);
#else
			_currentPoint = _pointerEventArgs.CurrentPoint;
#endif

			FrameId = pointerEventArgs.CurrentPoint.FrameId;
			Pointer = GetPointer(pointerEventArgs);
			KeyModifiers = pointerEventArgs.KeyModifiers;
			OriginalSource = source;
		}

		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			if (relativeTo is null)
			{
				return _currentPoint;
			}
			else
			{
				var absolutePosition = _pointerEventArgs.CurrentPoint.Position;
				// This is the same as relativeTo.TransformToVisual(null).Inverse.TransformPoint(absolutePosition);
				// However, TransformToVisual will allocate a MatrixTransform object, while this version is allocation-less
				var relativePosition = UIElement.GetTransform(from: relativeTo, to: null).Inverse().Transform(absolutePosition);

				return _currentPoint.At(relativePosition);
			}
		}

		private Pointer GetPointer(PointerEventArgs args)
			=> new Pointer(
				args.CurrentPoint.PointerId,
				(PointerDeviceType)args.CurrentPoint.PointerDevice.PointerDeviceType,
				isInContact: args.CurrentPoint.IsInContact,
				isInRange: args.CurrentPoint.Properties.IsInRange);
	}
}
#endif
