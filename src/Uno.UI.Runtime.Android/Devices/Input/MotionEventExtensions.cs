using Android.Views;
using Windows.System;
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;

namespace Uno.UI.Runtime.Android;

internal static class MotionEventExtensions
{
	public static PointerDeviceType ToPointerDeviceType(this MotionEventToolType nativeType)
		=> nativeType switch
		{
			MotionEventToolType.Eraser or MotionEventToolType.Stylus => PointerDeviceType.Pen,
			MotionEventToolType.Mouse => PointerDeviceType.Mouse,
			_ => PointerDeviceType.Touch, // Unknown is used by Xamarin.UITest
		};

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
		if ((nativeKeys & MetaKeyStates.AltMask) != 0)
		{
			keys |= VirtualKeyModifiers.Menu;
		}
		if ((nativeKeys & MetaKeyStates.MetaMask) != 0)
		{
			keys |= VirtualKeyModifiers.Windows;
		}

		return keys;
	}
}
