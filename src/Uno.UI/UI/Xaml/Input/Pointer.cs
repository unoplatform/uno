using System;
using System.Collections.Generic;
using System.Text;
using Uno;
using Windows.Devices.Input;

namespace Windows.UI.Xaml.Input
{
	public partial class Pointer : IEquatable<Pointer>
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

#if __WASM__ || XAMARIN_ANDROID
		public uint PointerId { get; private set; }
#else
		[NotImplemented]
		public uint PointerId => 1;
#endif

		public override string ToString()
		{
			return $"{PointerDeviceType}/{PointerId}";
		}

		public bool Equals(Pointer other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return PointerDeviceType == other.PointerDeviceType && PointerId == other.PointerId;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((Pointer) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((int) PointerDeviceType * 397) ^ (int) PointerId;
			}
		}
	}
}
