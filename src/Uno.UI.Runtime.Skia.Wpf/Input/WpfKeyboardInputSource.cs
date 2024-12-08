using System.Windows.Input;
using System;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Uno.UI.Helpers;
using Uno.UI.Hosting;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfUIElement = System.Windows.UIElement;
using WinUIKeyEventArgs = Windows.UI.Core.KeyEventArgs;
using System.Runtime.InteropServices;

namespace Uno.UI.Runtime.Skia.Wpf.Input;

internal class WpfKeyboardInputSource : IUnoKeyboardInputSource
{
	public event TypedEventHandler<object, WinUIKeyEventArgs> KeyDown;
	public event TypedEventHandler<object, WinUIKeyEventArgs> KeyUp;

	public WpfKeyboardInputSource(IXamlRootHost host)
	{
		if (host is null) return;

		if (host is not WpfUIElement hostControl)
		{
			throw new ArgumentException($"{nameof(host)} must be a WPF Control instance", nameof(host));
		}

		hostControl.AddHandler(WpfUIElement.KeyUpEvent, (KeyEventHandler)HostOnKeyUp, true);
		hostControl.AddHandler(WpfUIElement.KeyDownEvent, (KeyEventHandler)HostOnKeyDown, true);
	}

	private void HostOnKeyDown(object sender, KeyEventArgs args)
	{
		try
		{
			var virtualKey = ConvertKey(args.Key, args.SystemKey);
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"OnKeyPressEvent: {args.Key} -> {virtualKey}");
			}

			var scanCode = GetScanCode(virtualKey);

			KeyDown?.Invoke(this, new(
				"keyboard",
				virtualKey,
				GetKeyModifiers(args.KeyboardDevice.Modifiers),
				new CorePhysicalKeyStatus
				{
					ScanCode = scanCode,
					RepeatCount = 1,
				},
				KeyCodeToUnicode(scanCode, virtualKey)));
		}
		catch (Exception e)
		{
			Windows.UI.Xaml.Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	private void HostOnKeyUp(object sender, System.Windows.Input.KeyEventArgs args)
	{
		try
		{
			var virtualKey = ConvertKey(args.Key, args.SystemKey);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"OnKeyPressEvent: {args.Key} -> {virtualKey}");
			}

			var scanCode = GetScanCode(virtualKey);

			KeyUp?.Invoke(this, new(
				"keyboard",
				virtualKey,
				GetKeyModifiers(args.KeyboardDevice.Modifiers),
				new CorePhysicalKeyStatus
				{
					ScanCode = scanCode,
					RepeatCount = 1,
				},
				KeyCodeToUnicode(scanCode, virtualKey)));
		}
		catch (Exception e)
		{
			Windows.UI.Xaml.Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	private static uint GetScanCode(VirtualKey virtualKey)
	{
		var scanCode = MapVirtualKeyW((uint)virtualKey, 0 /*MAPVK_VK_TO_VSC*/);
		return scanCode;
	}

	private static char? KeyCodeToUnicode(uint keyCode, VirtualKey virtualKey)
	{
		var result = InputHelper.WindowsScancodeToUnicode(keyCode);

		// WindowsScancodeToUnicode doesn't pick up Numpad keys
		return virtualKey switch
		{
			VirtualKey.NumberPad0 => '0',
			VirtualKey.NumberPad1 => '1',
			VirtualKey.NumberPad2 => '2',
			VirtualKey.NumberPad3 => '3',
			VirtualKey.NumberPad4 => '4',
			VirtualKey.NumberPad5 => '5',
			VirtualKey.NumberPad6 => '6',
			VirtualKey.NumberPad7 => '7',
			VirtualKey.NumberPad8 => '8',
			VirtualKey.NumberPad9 => '9',
			VirtualKey.Decimal => '.',
			_ => result.Length > 0 ? result[0] : null // TODO: supplementary code points
		};
	}

	private static VirtualKeyModifiers GetKeyModifiers(ModifierKeys modifierKeys)
	{
		var modifiers = VirtualKeyModifiers.None;
		if (modifierKeys.HasFlag(ModifierKeys.Shift))
		{
			modifiers |= VirtualKeyModifiers.Shift;
		}
		if (modifierKeys.HasFlag(ModifierKeys.Control))
		{
			modifiers |= VirtualKeyModifiers.Control;
		}
		if (modifierKeys.HasFlag(ModifierKeys.Windows))
		{
			modifiers |= VirtualKeyModifiers.Windows;
		}
		return modifiers;
	}

	private static VirtualKey ConvertKey(Key key, Key systemKey)
	{
		if (key == Key.System)
		{
			return (VirtualKey)System.Windows.Input.KeyInterop.VirtualKeyFromKey(systemKey);
		}

		return (VirtualKey)System.Windows.Input.KeyInterop.VirtualKeyFromKey(key);
	}

	[DllImport("User32.dll")]
	private static extern uint MapVirtualKeyW(uint code, uint mapType);
}
