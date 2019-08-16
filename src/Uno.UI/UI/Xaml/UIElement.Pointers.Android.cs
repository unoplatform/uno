using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Input;
using Windows.UI.Input;
using Windows.UI.Xaml.Input;
using Android.Runtime;
using Android.Views;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Xaml
{
	partial class UIElement
	{
		private class TouchReRouter : Java.Lang.Object, View.IOnTouchListener
		{
			private readonly UIElement _target;

			public TouchReRouter(IntPtr handle, JniHandleOwnership transfer)
				: base(handle, transfer)
			{
			}

			private TouchReRouter(UIElement target)
				: base(IntPtr.Zero, JniHandleOwnership.DoNotRegister)
			{
				_target = target;
			}

			/// <inheritdoc />
			public bool OnTouch(View view, MotionEvent nativeEvent)
				=> _target.OnNativeTouch(nativeEvent);
		}

		//private class TouchListener : Java.Lang.Object, View.IOnTouchListener
		//{
		//	private static TouchListener Instance { get; } = new TouchListener();

		//	public TouchListener(IntPtr handle, JniHandleOwnership transfer)
		//		: base(handle, transfer)
		//	{
		//	}

		//	private TouchListener()
		//		: base(IntPtr.Zero, JniHandleOwnership.DoNotRegister)
		//	{
		//	}

		//	/// <inheritdoc />
		//	public bool OnTouch(View view, MotionEvent nativeEvent)
		//		=> view is UIElement elt && elt.OnNativeTouch(nativeEvent);
		//}

		private class PointerListener
		{

		}

		partial void AddPointerHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo)
		{
			if (handlersCount == 1)
			{
				// ??? this.SetOnTouchListener(TouchListener.Instance);

				// TODO: Enable pointer events reporting
			}
		}

		partial void RemovePointerHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler)
		{
			if (remainingHandlersCount == 0)
			{
				// TODO: Disable pointer events reporting
			}
		}

		protected sealed override bool OnNativeTouch(MotionEvent nativeEvent)
		{
			var args = new PointerRoutedEventArgs(nativeEvent, this);
			switch (nativeEvent.ActionMasked)
			{
				case MotionEventActions.HoverEnter:
					return OnNativePointerEnter(args);
				case MotionEventActions.HoverExit:
					return OnNativePointerExited(args);

				case MotionEventActions.Down when args.Pointer.PointerDeviceType == PointerDeviceType.Touch:
				case MotionEventActions.PointerDown when args.Pointer.PointerDeviceType == PointerDeviceType.Touch:
					return OnNativePointerEnter(args) | OnNativePointerDown(args);
				case MotionEventActions.Down:
				case MotionEventActions.PointerDown:
					return OnNativePointerDown(args);
				case MotionEventActions.Up when args.Pointer.PointerDeviceType == PointerDeviceType.Touch:
				case MotionEventActions.PointerUp when args.Pointer.PointerDeviceType == PointerDeviceType.Touch:
					return OnNativePointerUp(args) | OnNativePointerExited(args);
				case MotionEventActions.Up:
				case MotionEventActions.PointerUp:
					return OnNativePointerUp(args);

				case MotionEventActions.Move:
				case MotionEventActions.HoverMove:
					return OnNativePointerMove(args);

				case MotionEventActions.Cancel:
					return OnNativePointerCancel(args, isSwallowedBySystem: true /* TODO: Check if we can determine the real reason */);

				default:
					return false;
			}
		}

		#region Capture
		// No needs to explicitly capture pointers on Android, they are implicitly captured
		// partial void CapturePointerNative(Pointer pointer);
		// partial void ReleasePointerCaptureNative(Pointer pointer);
		#endregion

		partial void OnIsHitTestVisibleChangedPartial(bool oldValue, bool newValue)
		{
			base.SetNativeHitTestVisible(newValue);
		}

		// This section is using the UnoViewGroup overrides for performance reasons
		// where most of the work is performed on the java side.

		protected override bool NativeHitCheck()
			=> IsViewHit();
	}
}
