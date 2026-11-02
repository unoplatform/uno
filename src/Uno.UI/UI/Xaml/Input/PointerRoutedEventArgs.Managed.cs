#if UNO_HAS_MANAGED_POINTERS
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Windows.Devices.Input;
using Uno;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Core;

using PointerEventArgs = Windows.UI.Core.PointerEventArgs;
using Uno.Extensions;

using Microsoft.UI.Input;
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;

namespace Microsoft.UI.Xaml.Input
{
	partial class PointerRoutedEventArgs
	{
		private readonly PointerEventArgs _pointerEventArgs;
		private readonly PointerPoint _currentPoint;

		internal PointerEventArgs CoreArgs => _pointerEventArgs;

		internal PointerRoutedEventArgs(
			PointerEventArgs pointerEventArgs,
			UIElement source)
			: this(pointerEventArgs, GetWindowPoint(pointerEventArgs), source)
		{
		}

		/// <summary>
		/// Creates the args of the same pointer event for another original source, with the point of <paramref name="args"/>
		/// rather than one converted again, which could differ if a handler moved the root of a scoped InputInjector.
		/// </summary>
		internal PointerRoutedEventArgs(PointerRoutedEventArgs args, UIElement source)
			: this(args._pointerEventArgs, args._currentPoint, source)
		{
		}

		private PointerRoutedEventArgs(PointerEventArgs pointerEventArgs, PointerPoint currentPoint, UIElement source) : this()
		{
			_pointerEventArgs = pointerEventArgs;
			_currentPoint = currentPoint;

			FrameId = pointerEventArgs.CurrentPoint.FrameId;
			Pointer = GetPointer(pointerEventArgs);
			KeyModifiers = pointerEventArgs.KeyModifiers;
			Handled = pointerEventArgs.Handled;
			OriginalSource = source;
		}

		public bool Handled
		{
			get => _pointerEventArgs.Handled;
			set => _pointerEventArgs.Handled = value;
		}

		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			if (relativeTo is null)
			{
				return _currentPoint;
			}
			else
			{
				var absolutePosition = _currentPoint.Position;
				// This is the same as relativeTo.TransformToVisual(null).Inverse.TransformPoint(absolutePosition);
				// However, TransformToVisual will allocate a MatrixTransform object, while this version is allocation-less
				var relativePosition = UIElement.GetTransform(from: relativeTo, to: null).Inverse().Transform(absolutePosition);

				return _currentPoint.At(relativePosition);
			}
		}

		// A scoped InputInjector gives positions relative to its root: bring them to the window.
		private static PointerPoint GetWindowPoint(PointerEventArgs args)
		{
			var point = args.CurrentPoint;
			if (args.RelativeRoot is UIElement relativeRoot)
			{
				var windowPosition = UIElement.GetTransform(from: relativeRoot, to: null).Transform(point.Position);
				return point.At(windowPosition, windowPosition);
			}

			return point;
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
