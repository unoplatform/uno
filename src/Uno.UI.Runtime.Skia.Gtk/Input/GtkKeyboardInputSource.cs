#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Text;
using Gdk;
using Gtk;
using Windows.System;
using Windows.UI.Core;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Uno.UI.Hosting;
using Uno.UI.Helpers;

namespace Uno.UI.Runtime.Skia.Gtk;

partial class GtkKeyboardInputSource : IUnoKeyboardInputSource
{
	public event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<object, KeyEventArgs>? KeyUp;

	public GtkKeyboardInputSource(IXamlRootHost xamlRootHost)
	{
		global::Gtk.Key.SnooperInstall(OnKeySnoop); // TODO:MZ: Install snooper only once, make host-specific
	}

	private int OnKeySnoop(Widget grab_widget, EventKey e)
	{
		if (e.Type == EventType.KeyPress)
		{
			OnKeyPressEvent(e);
		}
		else if (e.Type == EventType.KeyRelease)
		{
			OnKeyReleaseEvent(e);
		}

		return 0;
	}

	private void OnKeyPressEvent(EventKey evt)
	{
		try
		{
			var virtualKey = ConvertKey(evt);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"OnKeyPressEvent: {evt.Key} -> {virtualKey}");
			}

			InputHelper.TryConvertKeyCodeToScanCode(evt.HardwareKeycode, out var scanCode);

			KeyDown?.Invoke(this, new(
					"keyboard",
					virtualKey,
					GetKeyModifiers(evt.State),
					new CorePhysicalKeyStatus
					{
						ScanCode = scanCode,
						RepeatCount = 1,
					},
					KeyCodeToUnicode(evt.HardwareKeycode, evt.KeyValue)));
		}
		catch (Exception e)
		{
			Microsoft.UI.Xaml.Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	private void OnKeyReleaseEvent(EventKey evt)
	{
		try
		{
			var virtualKey = ConvertKey(evt);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"OnKeyReleaseEvent: {evt.Key} -> {virtualKey}");
			}

			InputHelper.TryConvertKeyCodeToScanCode(evt.HardwareKeycode, out var scanCode);

			KeyUp?.Invoke(this, new(
					"keyboard",
					virtualKey,
					GetKeyModifiers(evt.State),
					new CorePhysicalKeyStatus
					{
						ScanCode = scanCode,
						RepeatCount = 1,
					},
					KeyCodeToUnicode(evt.HardwareKeycode, evt.KeyValue)));
		}
		catch (Exception e)
		{
			Microsoft.UI.Xaml.Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	private static char? KeyCodeToUnicode(uint keyCode, uint keyVal)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			var result = InputHelper.WindowsKeyCodeToUnicode(keyCode);
			return result.Length > 0 ? result[0] : null; // TODO: supplementary code points
		}

		var gdkChar = (char)Keyval.ToUnicode(keyVal);

		return gdkChar == 0 ? null : gdkChar;
	}

	private static VirtualKey ConvertKey(EventKey e)
	{
		return (VirtualKey)e.HardwareKeycode;
	}

	internal static VirtualKeyModifiers GetKeyModifiers(Gdk.ModifierType state)
	{
		var modifiers = VirtualKeyModifiers.None;
		if (state.HasFlag(Gdk.ModifierType.ShiftMask))
		{
			modifiers |= VirtualKeyModifiers.Shift;
		}
		if (state.HasFlag(Gdk.ModifierType.ControlMask))
		{
			modifiers |= VirtualKeyModifiers.Control;
		}
		if (state.HasFlag(Gdk.ModifierType.Mod1Mask))
		{
			modifiers |= VirtualKeyModifiers.Menu;
		}
		return modifiers;
	}
}
