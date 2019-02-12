using Android.Views;
using Windows.Foundation;
using Windows.UI.Xaml.Extensions;
using Windows.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Uno.Extensions;

namespace Windows.UI.Xaml
{
	internal sealed class GestureHandler : UnoGestureDetector
	{
		private UIElement _target;

		internal void UpdateShouldHandle(RoutedEvent routedEvent, bool shouldHandle)
		{
			// TODO: How expensive is this?
			if (routedEvent == UIElement.PointerPressedEvent)
			{
				SetShouldHandleDown(shouldHandle);
			}
			else if (routedEvent == UIElement.PointerReleasedEvent)
			{
				SetShouldHandleUp(shouldHandle);
			}
			else if (routedEvent == UIElement.PointerMovedEvent)
			{
				SetShouldHandleMove(shouldHandle);
			}
			else if (routedEvent == UIElement.PointerEnteredEvent)
			{
				SetShouldHandleEnter(shouldHandle);
			}
			else if (routedEvent == UIElement.PointerExitedEvent)
			{
				SetShouldHandleExit(shouldHandle);
			}
			else if (routedEvent == UIElement.PointerCanceledEvent)
			{
				SetShouldHandleCancel(shouldHandle);
			}
			else if (routedEvent == UIElement.TappedEvent)
			{
				SetShouldHandleSingleTap(shouldHandle);
			}
			else if (routedEvent == UIElement.DoubleTappedEvent)
			{
				SetShouldHandleDoubleTap(shouldHandle);
			}
		}

		internal static GestureHandler Create(UIElement target)
		{
			var gestureHandler = new GestureHandler(target);

			gestureHandler.Configure(target);

			return gestureHandler;
		}

		private GestureHandler(UIElement target)
			: base(ContextHelper.Current, target)
		{
			_target = target;
		}

		protected override bool OnSingleTap(MotionEvent e)
		{
			try
			{
				var pointer = e.GetPointer(0);
				var args = new TappedRoutedEventArgs(new Point(e.GetX(), e.GetY()))
				{
					OriginalSource = this,
					PointerDeviceType = pointer.PointerDeviceType,
					CanBubbleNatively = true
				};

				return _target.RaiseEvent(UIElement.TappedEvent, args);
			}
			catch (Exception ex)
			{
				Windows.UI.Xaml.Application.Current.RaiseRecoverableUnhandledExceptionOrLog(ex, this);
				return false;
			}
		}

		protected override bool OnDoubleTap(MotionEvent e)
		{
			try
			{
				var pointer = e.GetPointer(0);

				var args = new DoubleTappedRoutedEventArgs(new Point(e.GetX(), e.GetY()))
				{
					OriginalSource = this,
					PointerDeviceType = pointer.PointerDeviceType,
					CanBubbleNatively = true
				};

				return _target.RaiseEvent(UIElement.DoubleTappedEvent, args);
			}
			catch (Exception ex)
			{
				Windows.UI.Xaml.Application.Current.RaiseRecoverableUnhandledExceptionOrLog(ex, this);
				return false;
			}
		}

		protected override bool OnDown(MotionEvent e)
		{
			try
			{
				var args = GetPointerEventArgs(e, 0);

				return _target.RaiseEvent(UIElement.PointerPressedEvent, args);
			}
			catch (Exception ex)
			{
				Windows.UI.Xaml.Application.Current.RaiseRecoverableUnhandledExceptionOrLog(ex, this);
				return false;
			}
		}

		protected override bool OnUp(MotionEvent e)
		{
			try
			{
				var args = GetPointerEventArgs(e, 0);

				return _target.RaiseEvent(UIElement.PointerReleasedEvent, args);
			}
			catch (Exception ex)
			{
				Windows.UI.Xaml.Application.Current.RaiseRecoverableUnhandledExceptionOrLog(ex, this);
				return false;
			}
		}

		protected override bool OnMove(MotionEvent e)
		{
			try
			{
				var args = GetPointerEventArgs(e, 0);

				return _target.RaiseEvent(UIElement.PointerMovedEvent, args);
			}
			catch (Exception ex)
			{
				Windows.UI.Xaml.Application.Current.RaiseRecoverableUnhandledExceptionOrLog(ex, this);
				return false;
			}
		}

		protected override bool OnExit(MotionEvent e)
		{
			try
			{
				var args = GetPointerEventArgs(e, 0);

				return _target.RaiseEvent(UIElement.PointerExitedEvent, args);
			}
			catch (Exception ex)
			{
				Windows.UI.Xaml.Application.Current.RaiseRecoverableUnhandledExceptionOrLog(ex, this);
				return false;
			}
		}

		protected override bool OnEnter(MotionEvent p0)
		{
			try
			{
				var args = GetPointerEventArgs(p0, 0);

				return _target.RaiseEvent(UIElement.PointerEnteredEvent, args);
			}
			catch (Exception ex)
			{
				Windows.UI.Xaml.Application.Current.RaiseRecoverableUnhandledExceptionOrLog(ex, this);
				return false;
			}
		}

		protected override bool OnCancel(MotionEvent e)
		{
			try
			{
				var args = GetPointerEventArgs(e, 0);

				return _target.RaiseEvent(UIElement.PointerCanceledEvent, args);
			}
			catch (Exception ex)
			{
				Windows.UI.Xaml.Application.Current.RaiseRecoverableUnhandledExceptionOrLog(ex, this);
				return false;
			}
		}

		private PointerRoutedEventArgs GetPointerEventArgs(MotionEvent ev, int pointerIndex)
		{
			return new PointerRoutedEventArgs(ev)
			{
				OriginalSource = this,
				Pointer = ev.GetPointer(pointerIndex),
				CanBubbleNatively = true
			};
		}
	}
}
