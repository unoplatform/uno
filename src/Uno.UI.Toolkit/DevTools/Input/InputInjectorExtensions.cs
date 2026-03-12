using System;

#nullable enable

using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Input;

#if !HAS_UNO
using System.Runtime.InteropServices;
#endif

#if HAS_UNO_WINUI || WINAPPSDK
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

namespace Uno.UI.Toolkit.DevTools.Input;

internal static class InputInjectorExtensions
{
	public static IInjectedPointer GetPointer(this InputInjector injector, PointerDeviceType pointer)
		=> pointer switch
		{
			PointerDeviceType.Touch => GetFinger(injector),
#if HAS_UNO
			PointerDeviceType.Mouse => GetMouse(injector),
			PointerDeviceType.Pen => GetPen(injector),
#endif
			_ => throw new NotSupportedException($"Injection of {pointer} is not supported on this platform.")
		};

	public static Finger GetFinger(this InputInjector injector, uint id = 42)
		=> new(injector, id);

#if HAS_UNO
	public static Mouse GetMouse(this InputInjector injector)
		=> new(injector);

	public static Pen GetPen(this InputInjector injector, uint id = 1)
		=> new(injector, id);
#endif
}
