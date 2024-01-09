#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;

using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSUnoKeyboardInputSource : IUnoKeyboardInputSource
{
	public static MacOSUnoKeyboardInputSource Instance = new();

	private unsafe MacOSUnoKeyboardInputSource()
	{
	}

	public static unsafe void Register()
	{
		// FIXME: use a single `uno_set_window_key_callbacks` call ?
		NativeUno.uno_set_window_key_down_callback(&OnRawKeyDown);
		NativeUno.uno_set_window_key_up_callback(&OnRawKeyUp);
		ApiExtensibility.Register(typeof(IUnoKeyboardInputSource), o => Instance);
	}

#pragma warning disable CS0067

	public event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<object, KeyEventArgs>? KeyUp;

#pragma warning restore CS0067

	private static KeyEventArgs CreateArgs(VirtualKey key, VirtualKeyModifiers mods, uint scanCode)
	{
		var status = new CorePhysicalKeyStatus
		{
			ScanCode = scanCode,
		};
		return new KeyEventArgs("keyboard", key, mods, status);
	}

	[UnmanagedCallersOnly(CallConvs = new[] {typeof(CallConvCdecl)})]
	private static int OnRawKeyDown(VirtualKey key, VirtualKeyModifiers mods, uint scanCode)
	{
		try
		{
			if (typeof(MacOSUnoKeyboardInputSource).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(MacOSUnoKeyboardInputSource).Log().Trace($"OnRawKeyDown '${key}', mods: '{mods}', scanCode: {scanCode}");
			}

			var keydown = Instance.KeyDown;
			if (keydown is null)
			{
				return 0;
			}
			var args = CreateArgs(key, mods, scanCode);
			keydown.Invoke(Instance, args);
			return args.Handled ? 1 : 0;
		}
		catch (Exception e)
		{
			Application.Current.RaiseRecoverableUnhandledException(e);
			return 0;
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] {typeof(CallConvCdecl)})]
	private static int OnRawKeyUp(VirtualKey key, VirtualKeyModifiers mods, uint scanCode)
	{
		try
		{
			if (typeof(MacOSUnoKeyboardInputSource).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(MacOSUnoKeyboardInputSource).Log().Trace($"OnRawKeyUp '${key}', mods: '{mods}', scanCode: {scanCode}");
			}

			var keyup = Instance.KeyUp;
			if (keyup is null)
			{
				return 0;
			}
			var args = CreateArgs(key, mods, scanCode);
			keyup.Invoke(Instance, args);
			return args.Handled ? 1 : 0;
		}
		catch (Exception e)
		{
			Application.Current.RaiseRecoverableUnhandledException(e);
			return 0;
		}
	}
}
