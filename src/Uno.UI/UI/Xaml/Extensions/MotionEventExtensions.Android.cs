using Android.Views;
using Windows.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Input;

namespace Windows.UI.Xaml.Extensions
{
	internal static class MotionEventExtensions
	{
		public static PointerDeviceType ToPointerDeviceType(this MotionEventToolType nativeType)
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

		public static VirtualKeyModifiers ToVirtualKeyModifiers(this MetaKeyStates nativeKeys)
		{
			var keys = VirtualKeyModifiers.None;

			if ((nativeKeys & MetaKeyStates.ShiftMask) != 0)
			{
				keys |= VirtualKeyModifiers.Shift;
			}
			if ((nativeKeys & MetaKeyStates.CtrlMask) != 0)
			{
				keys |= VirtualKeyModifiers.Control;
			}
			if ((nativeKeys & MetaKeyStates.MetaMask) != 0)
			{
				keys |= VirtualKeyModifiers.Windows;
			}

			return keys;
		}
	}
}
