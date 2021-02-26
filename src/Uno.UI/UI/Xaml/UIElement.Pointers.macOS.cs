using CoreAnimation;
using CoreGraphics;
using Foundation;
using Uno.Extensions;
using Uno.UI.Controls;
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Logging;
using Uno;
using Windows.Devices.Input;
using Microsoft.Extensions.Logging;
using Windows.UI.Xaml.Media;
using Uno.UI.Extensions;
using Uno.UI;
using AppKit;
using Uno.UI.Xaml;

namespace Windows.UI.Xaml
{
	public partial class UIElement : BindableNSView
	{
		private bool _hasRegisteredTrackingArea;

		partial void AddPointerHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo)
		{
			// Note: we are enabling tracking also for pressed and released in order to always maintain a coherent state.
			const RoutedEventFlag needsTracking = RoutedEventFlag.PointerPressed
				| RoutedEventFlag.PointerReleased
				| RoutedEventFlag.PointerEntered
				| RoutedEventFlag.PointerExited
				| RoutedEventFlag.PointerMoved;

			if (!_hasRegisteredTrackingArea
				&& (routedEvent.Flag & needsTracking) != default)
			{
				var options = NSTrackingAreaOptions.MouseEnteredAndExited
					| NSTrackingAreaOptions.MouseMoved
					| NSTrackingAreaOptions.ActiveInKeyWindow
					| NSTrackingAreaOptions.EnabledDuringMouseDrag // We want enter/leave events even if the button is pressed
					| NSTrackingAreaOptions.InVisibleRect; // Automagicaly syncs the bounds rect
				var trackingArea = new NSTrackingArea(Bounds, options, this, null);

				AddTrackingArea(trackingArea);
				_hasRegisteredTrackingArea = true;
			}
		}

		public override void MouseDown(NSEvent evt)
		{
			if (IsPointersSuspended)
			{
				return;
			}

			try
			{
				// evt.AllTouches raises a invalid selector exception
				var args = new PointerRoutedEventArgs(null, evt, this);

				var pointerEventIsHandledInManaged = OnNativePointerDown(args);

				if (!pointerEventIsHandledInManaged)
				{
					// Bubble up the event natively
					base.MouseDown(evt);
				}
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}

		public override void MouseMoved(NSEvent evt)
		{
			if (IsPointersSuspended)
			{
				return;
			}

			try
			{
				// evt.AllTouches raises a invalid selector exception
				var args = new PointerRoutedEventArgs(null, evt, this);

				var pointerEventIsHandledInManaged = OnNativePointerMove(args);

				if (!pointerEventIsHandledInManaged)
				{
					// Bubble up the event natively
					base.MouseMoved(evt);
				}
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}

		public override void MouseEntered(NSEvent evt)
		{
			if (IsPointersSuspended)
			{
				return;
			}

			try
			{
				var args = new PointerRoutedEventArgs(null, evt, this);

				var pointerEventIsHandledInManaged = OnNativePointerEnter(args);

				if (!pointerEventIsHandledInManaged)
				{
					// Bubble up the event natively
					base.MouseEntered(evt);
				}
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}

		public override void MouseExited(NSEvent evt)
		{
			if (IsPointersSuspended)
			{
				return;
			}

			try
			{
				// evt.AllTouches raises a invalid selector exception
				var args = new PointerRoutedEventArgs(null, evt, this);

				var pointerEventIsHandledInManaged = OnNativePointerExited(args);

				if (!pointerEventIsHandledInManaged)
				{
					// Bubble up the event natively
					base.MouseExited(evt);
				}
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}

		public override void MouseUp(NSEvent evt)
		{
			if (IsPointersSuspended)
			{
				return;
			}

			try
			{
				// evt.AllTouches raises a invalid selector exception
				var args = new PointerRoutedEventArgs(null, evt, this);

				var pointerEventIsHandledInManaged = OnNativePointerUp(args);

				if (!pointerEventIsHandledInManaged)
				{
					// Bubble up the event natively
					base.MouseUp(evt);
				}
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}

		public override void MouseDragged(NSEvent evt)
		{
			if (IsPointersSuspended)
			{
				return;
			}

			try
			{
				// evt.AllTouches raises a invalid selector exception
				var args = new PointerRoutedEventArgs(null, evt, this);

				var pointerEventIsHandledInManaged = OnNativePointerMoveWithOverCheck(args, evt.IsTouchInView(this));

				if (!pointerEventIsHandledInManaged)
				{
					// Bubble up the event natively
					base.MouseDragged(evt);
				}
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}

		public override void ScrollWheel(NSEvent evt)
		{
			if (IsPointersSuspended)
			{
				return;
			}

			try
			{
				// evt.AllTouches raises a invalid selector exception
				var args = new PointerRoutedEventArgs(null, evt, this);

				var pointerEventIsHandledInManaged = OnNativePointerWheel(args);

				if (!pointerEventIsHandledInManaged)
				{
					// Bubble up the event natively
					base.ScrollWheel(evt);
				}
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}
	}
}
