using System.Windows.Input;
using System;
using System.Reflection;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Uno.UI.Helpers;
using Uno.UI.Hosting;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfUIElement = System.Windows.UIElement;
using WinUIKeyEventArgs = Windows.UI.Core.KeyEventArgs;

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
			var virtualKey = ConvertKey(args.Key);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"OnKeyPressEvent: {args.Key} -> {virtualKey}");
			}

			var scanCode = GetScanCode(args);

			KeyDown?.Invoke(this, new(
				"keyboard",
				virtualKey,
				GetKeyModifiers(args.KeyboardDevice.Modifiers),
				new CorePhysicalKeyStatus
				{
					ScanCode = scanCode,
					RepeatCount = 1,
				},
				KeyCodeToUnicode(scanCode)));
		}
		catch (Exception e)
		{
			Microsoft.UI.Xaml.Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	private void HostOnKeyUp(object sender, System.Windows.Input.KeyEventArgs args)
	{
		try
		{
			var virtualKey = ConvertKey(args.Key);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"OnKeyPressEvent: {args.Key} -> {virtualKey}");
			}

			var scanCode = GetScanCode(args);

			KeyUp?.Invoke(this, new(
				"keyboard",
				virtualKey,
				GetKeyModifiers(args.KeyboardDevice.Modifiers),
				new CorePhysicalKeyStatus
				{
					ScanCode = scanCode,
					RepeatCount = 1,
				},
				KeyCodeToUnicode(scanCode)));
		}
		catch (Exception e)
		{
			Microsoft.UI.Xaml.Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	// WPF doesn't expose the scancode, but it exists internally
	private static uint GetScanCode(KeyEventArgs args)
	{
		try
		{
			if (typeof(KeyEventArgs).GetProperty("ScanCode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) is { } propertyInfo)
			{
				return (uint)(int)propertyInfo.GetValue(args)!;
			}
			else
			{
				throw new PlatformNotSupportedException("Unable to get the ScanCode property from WPF. This likely means this WPF version is not compatible with Uno Platform, contact the developers for more information.");
			}
		}
		catch (Exception e)
		{
			if (typeof(WpfKeyboardInputSource).Log().IsEnabled(LogLevel.Error))
			{
				typeof(WpfKeyboardInputSource).Log().LogError("Unable to get ScanCode from WPF KeyEventArgs.", e);
			}

			throw;
		}
	}

	private static char? KeyCodeToUnicode(uint keyCode)
	{
		var result = InputHelper.WindowsScancodeToUnicode(keyCode);
		return result.Length > 0 ? result[0] : null; // TODO: supplementary code points
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

	private VirtualKey ConvertKey(Key key)
	{
		return (VirtualKey)System.Windows.Input.KeyInterop.VirtualKeyFromKey(key);
	}
}
