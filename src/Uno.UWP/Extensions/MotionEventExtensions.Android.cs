using System;
using System.Collections.Generic;
using System.Text;
using Android.Views;
using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Input;

namespace Uno.UI.Xaml.Extensions
{
	internal static class MotionEventExtensions
	{
		public static PointerDeviceType ToPointerDeviceType(this MotionEventToolType nativeType)
		{
			switch (nativeType)
			{
				case MotionEventToolType.Eraser:
				case MotionEventToolType.Stylus:
					return PointerDeviceType.Pen;
				case MotionEventToolType.Finger:
					return PointerDeviceType.Touch;
				case MotionEventToolType.Mouse:
					return PointerDeviceType.Mouse;
				case MotionEventToolType.Unknown: // used by Xamarin.UITest
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
