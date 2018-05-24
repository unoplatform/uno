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
		internal void RaiseTapped(object sender, TappedRoutedEventArgs args) => Tapped?.Invoke(sender, args);

		#region Tapped event
		private event TappedEventHandler Tapped;

		internal void RegisterTapped(TappedEventHandler handler)
		{
			Tapped += handler;
			SetShouldHandleSingleTap(true);
		}

		internal void UnregisterTapped(TappedEventHandler handler)
		{
			Tapped -= handler;
			SetShouldHandleSingleTap(Tapped != null);
		}
		#endregion

		#region DoubleTapped event
		private event DoubleTappedEventHandler DoubleTapped;

		internal void RegisterDoubleTapped(DoubleTappedEventHandler handler)
		{
			DoubleTapped += handler;
			SetShouldHandleDoubleTap(true);
		}

		internal void UnregisterDoubleTapped(DoubleTappedEventHandler handler)
		{
			DoubleTapped -= handler;
			SetShouldHandleDoubleTap(DoubleTapped != null);
		}
		#endregion

		#region PointerPressed event
		private event PointerEventHandler PointerPressed;

		internal void RegisterPointerPressed(PointerEventHandler handler)
		{
			PointerPressed += handler;
			SetShouldHandleDown(true);
		}

		internal void UnregisterPointerPressed(PointerEventHandler handler)
		{
			PointerPressed -= handler;
			SetShouldHandleDown(PointerPressed != null);
		}
		#endregion

		#region PointerReleased event
		private event PointerEventHandler PointerReleased;

		internal void RegisterPointerReleased(PointerEventHandler handler)
		{
			PointerReleased += handler;
			SetShouldHandleUp(true);
		}

		internal void UnregisterPointerReleased(PointerEventHandler handler)
		{
			PointerReleased -= handler;
			SetShouldHandleUp(PointerReleased != null);
		}
		#endregion

		#region PointerMoved event
		private event PointerEventHandler PointerMoved;

		internal void RegisterPointerMoved(PointerEventHandler handler)
		{
			PointerMoved += handler;
			SetShouldHandleMove(true);
		}

		internal void UnregisterPointerMoved(PointerEventHandler handler)
		{
			PointerMoved -= handler;
			SetShouldHandleMove(PointerMoved != null);
		}
		#endregion

		#region PointerExited event
		private event PointerEventHandler PointerExited;

		internal void RegisterPointerExited(PointerEventHandler handler)
		{
			PointerExited += handler;
			SetShouldHandleExit(true);
		}

		internal void UnregisterPointerExited(PointerEventHandler handler)
		{
			PointerExited -= handler;
			SetShouldHandleExit(PointerExited != null);
		}
		#endregion

		#region PointerEntered event
		private event PointerEventHandler PointerEntered;

		internal void RegisterPointerEntered(PointerEventHandler handler)
		{
			PointerEntered += handler;
			SetShouldHandleEnter(true);
		}

		internal void UnregisterPointerEntered(PointerEventHandler handler)
		{
			PointerEntered -= handler;
			SetShouldHandleEnter(PointerEntered != null);
		}
		#endregion

		#region PointerCanceled event
		private event PointerEventHandler PointerCanceled;

		internal void RegisterPointerCanceled(PointerEventHandler handler)
		{
			PointerCanceled += handler;
			SetShouldHandleCancel(true);
		}

		internal void UnregisterPointerCanceled(PointerEventHandler handler)
		{
			PointerCanceled -= handler;
			SetShouldHandleCancel(PointerCanceled != null);
		}
		#endregion

		internal static GestureHandler Create(UIElement target)
		{
			var gestureHandler = new GestureHandler(target);

			gestureHandler.Configure(target);

			return gestureHandler;
		}

		private GestureHandler(UIElement target)
			: base(ContextHelper.Current, target)
		{
		}

		protected override bool OnSingleTap(MotionEvent e)
		{
			try
			{
				var pointer = e.GetPointer(0);
				var args = new TappedRoutedEventArgs(new Point(e.GetX(), e.GetY()))
				{
					OriginalSource = this,
					PointerDeviceType = pointer.PointerDeviceType
				};

				Tapped?.Invoke(Target, args);

				return args.Handled;
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
					PointerDeviceType = pointer.PointerDeviceType
				};

				DoubleTapped?.Invoke(Target, args);

				return args.Handled;
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

				PointerPressed?.Invoke(Target, args);

				return args.Handled;
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

				PointerReleased?.Invoke(Target, args);

				return args.Handled;
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

				PointerMoved?.Invoke(Target, args);

				return args.Handled;
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

				PointerExited?.Invoke(Target, args);

				return args.Handled;
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

				PointerEntered?.Invoke(Target, args);

				return args.Handled;
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

				PointerCanceled?.Invoke(Target, args);

				return args.Handled;
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
				Pointer = ev.GetPointer(pointerIndex)
			};
		}
	}
}
