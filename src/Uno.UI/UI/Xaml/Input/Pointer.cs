using System;
using System.Collections.Generic;
using System.Text;
using Uno;
using Windows.Devices.Input;

namespace Windows.UI.Xaml.Input
{
	public partial class Pointer
	{

#if XAMARIN_ANDROID
		internal Pointer(Android.Views.MotionEvent.PointerProperties properties)
		{
			PointerId = (uint)properties.Id;
			PointerDeviceType = GetPointerType(properties.ToolType);
		}

		private static PointerDeviceType GetPointerType(Android.Views.MotionEventToolType nativeType)
		{
			switch (nativeType)
			{
				case Android.Views.MotionEventToolType.Eraser:
				case Android.Views.MotionEventToolType.Stylus:
					return PointerDeviceType.Pen;
				case Android.Views.MotionEventToolType.Finger:
					return PointerDeviceType.Touch;
				case Android.Views.MotionEventToolType.Mouse:
					return PointerDeviceType.Mouse;
				case Android.Views.MotionEventToolType.Unknown: // used by Xamarin.UITest
				default:
					return default(PointerDeviceType);
			}
		}
#elif __IOS__
		internal Pointer(UIKit.UIEvent uiEvent)
		{
			switch (uiEvent.Type)
			{
				case UIKit.UIEventType.Touches:
				case UIKit.UIEventType.Motion:
					PointerDeviceType = PointerDeviceType.Touch;
					break;

				case UIKit.UIEventType.Presses:
				case UIKit.UIEventType.RemoteControl:
					PointerDeviceType = PointerDeviceType.Pen;
					break;
			}
		}
#elif __MACOS__
		internal Pointer(AppKit.NSEvent uiEvent)
		{
			switch (uiEvent.Type)
			{
				case AppKit.NSEventType.DirectTouch:
					PointerDeviceType = PointerDeviceType.Touch;
					break;

				case AppKit.NSEventType.MouseMoved:
					PointerDeviceType = PointerDeviceType.Mouse;
					break;
			}
		}
#elif __WASM__
		internal Pointer(uint id, PointerDeviceType type)
		{
			PointerId = id;
			PointerDeviceType = type;
		}
#endif

		[NotImplemented]
		public bool IsInContact => true;

		[NotImplemented]
		public bool IsInRange => true;

		public PointerDeviceType PointerDeviceType { get; private set; }

#if !__WASM__
		[NotImplemented]
#endif
		public uint PointerId { get; private set; }
	}
}
