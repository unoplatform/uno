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

			var scanCode = EventHelper.GetScancode(evt);

			KeyDown?.Invoke(this, new(
					"keyboard",
					virtualKey,
					GetKeyModifiers(evt.State),
					new CorePhysicalKeyStatus
					{
						ScanCode = (uint)scanCode,
						RepeatCount = 1,
					},
					KeyCodeToUnicode(evt.HardwareKeycode, evt.KeyValue)));
		}
		catch (Exception e)
		{
			Windows.UI.Xaml.Application.Current.RaiseRecoverableUnhandledException(e);
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

			var scanCode = EventHelper.GetScancode(evt);

			KeyUp?.Invoke(this, new(
					"keyboard",
					virtualKey,
					GetKeyModifiers(evt.State),
					new CorePhysicalKeyStatus
					{
						ScanCode = (uint)scanCode,
						RepeatCount = 1,
					},
					KeyCodeToUnicode(evt.HardwareKeycode, evt.KeyValue)));
		}
		catch (Exception e)
		{
			Windows.UI.Xaml.Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	private static char? KeyCodeToUnicode(uint keyCode, uint keyVal)
	{
		if (OperatingSystem.IsWindows())
		{
			var result = InputHelper.WindowsKeyCodeToUnicode(keyCode);
			return result.Length > 0 ? result[0] : null; // TODO: supplementary code points
		}

		var gdkChar = (char)Keyval.ToUnicode(keyVal);

		return gdkChar == 0 ? null : gdkChar;
	}

	private static VirtualKey ConvertKey(EventKey e)
	{
		if (OperatingSystem.IsWindows())
		{
			// This doesn't work correctly on non-Windows.
			return (VirtualKey)e.HardwareKeycode;
		}

		// In this function, commented out lines correspond to VirtualKeys not yet
		// mapped to their native counterparts. Uncomment and fix as needed.

		return e.Key switch
		{
			Gdk.Key.VoidSymbol => VirtualKey.None,

			// Gdk.Key.Left => VirtualKey.LeftButton,
			// Gdk.Key.Right => VirtualKey.RightButton,
			Gdk.Key.Cancel => VirtualKey.Cancel,
			// Gdk.Key.MiddleButton => VirtualKey.MiddleButton,
			// Gdk.Key.XButton1 => VirtualKey.XButton1,
			// Gdk.Key.XButton2 => VirtualKey.XButton2,
			Gdk.Key.BackSpace => VirtualKey.Back,
			Gdk.Key.Tab => VirtualKey.Tab,
			Gdk.Key.ISO_Left_Tab => VirtualKey.Tab,
			Gdk.Key.KP_Tab => VirtualKey.Tab,
			Gdk.Key.Clear => VirtualKey.Clear,
			Gdk.Key.Return => VirtualKey.Enter,
			Gdk.Key.KP_Enter => VirtualKey.Enter,
			// Gdk.Key.Shift => VirtualKey.Shift,
			// Gdk.Key.Control => VirtualKey.Control,
			Gdk.Key.Menu => VirtualKey.Menu,
			Gdk.Key.Pause => VirtualKey.Pause,
			Gdk.Key.Caps_Lock => VirtualKey.CapitalLock,
			Gdk.Key.Kana_Lock => VirtualKey.Kana,
			Gdk.Key.Hangul => VirtualKey.Hangul,
			// Gdk.Key.Junja => VirtualKey.Junja,
			// Gdk.Key.Final => VirtualKey.Final,
			// Gdk.Key.Hanja => VirtualKey.Hanja,
			Gdk.Key.Kanji => VirtualKey.Kanji,
			Gdk.Key.Escape => VirtualKey.Escape,
			// Gdk.Key.Convert => VirtualKey.Convert,
			// Gdk.Key.NonConvert => VirtualKey.NonConvert,
			// Gdk.Key.Accept => VirtualKey.Accept,
			// Gdk.Key.ModeChange => VirtualKey.ModeChange,
			Gdk.Key.space => VirtualKey.Space,
			Gdk.Key.Page_Up => VirtualKey.PageUp,
			Gdk.Key.Page_Down => VirtualKey.PageDown,
			Gdk.Key.End => VirtualKey.End,
			Gdk.Key.Home => VirtualKey.Home,
			Gdk.Key.Left => VirtualKey.Left,
			Gdk.Key.Up => VirtualKey.Up,
			Gdk.Key.Right => VirtualKey.Right,
			Gdk.Key.Down => VirtualKey.Down,
			Gdk.Key.Select => VirtualKey.Select,
			Gdk.Key.Print => VirtualKey.Print,
			Gdk.Key.Execute => VirtualKey.Execute,
			// Gdk.Key.Snapshot => VirtualKey.Snapshot,
			Gdk.Key.Insert => VirtualKey.Insert,
			Gdk.Key.Delete => VirtualKey.Delete,
			Gdk.Key.Help => VirtualKey.Help,
			Gdk.Key.Key_0 => VirtualKey.Number0,
			Gdk.Key.Key_1 => VirtualKey.Number1,
			Gdk.Key.Key_2 => VirtualKey.Number2,
			Gdk.Key.Key_3 => VirtualKey.Number3,
			Gdk.Key.Key_4 => VirtualKey.Number4,
			Gdk.Key.Key_5 => VirtualKey.Number5,
			Gdk.Key.Key_6 => VirtualKey.Number6,
			Gdk.Key.Key_7 => VirtualKey.Number7,
			Gdk.Key.Key_8 => VirtualKey.Number8,
			Gdk.Key.Key_9 => VirtualKey.Number9,
			Gdk.Key.A => VirtualKey.A,
			Gdk.Key.B => VirtualKey.B,
			Gdk.Key.C => VirtualKey.C,
			Gdk.Key.D => VirtualKey.D,
			Gdk.Key.E => VirtualKey.E,
			Gdk.Key.F => VirtualKey.F,
			Gdk.Key.G => VirtualKey.G,
			Gdk.Key.H => VirtualKey.H,
			Gdk.Key.I => VirtualKey.I,
			Gdk.Key.J => VirtualKey.J,
			Gdk.Key.K => VirtualKey.K,
			Gdk.Key.L => VirtualKey.L,
			Gdk.Key.M => VirtualKey.M,
			Gdk.Key.N => VirtualKey.N,
			Gdk.Key.O => VirtualKey.O,
			Gdk.Key.P => VirtualKey.P,
			Gdk.Key.Q => VirtualKey.Q,
			Gdk.Key.R => VirtualKey.R,
			Gdk.Key.S => VirtualKey.S,
			Gdk.Key.T => VirtualKey.T,
			Gdk.Key.U => VirtualKey.U,
			Gdk.Key.V => VirtualKey.V,
			Gdk.Key.W => VirtualKey.W,
			Gdk.Key.X => VirtualKey.X,
			Gdk.Key.Y => VirtualKey.Y,
			Gdk.Key.Z => VirtualKey.Z,

			Gdk.Key.a => VirtualKey.A,
			Gdk.Key.b => VirtualKey.B,
			Gdk.Key.c => VirtualKey.C,
			Gdk.Key.d => VirtualKey.D,
			Gdk.Key.e => VirtualKey.E,
			Gdk.Key.f => VirtualKey.F,
			Gdk.Key.g => VirtualKey.G,
			Gdk.Key.h => VirtualKey.H,
			Gdk.Key.i => VirtualKey.I,
			Gdk.Key.j => VirtualKey.J,
			Gdk.Key.k => VirtualKey.K,
			Gdk.Key.l => VirtualKey.L,
			Gdk.Key.m => VirtualKey.M,
			Gdk.Key.n => VirtualKey.N,
			Gdk.Key.o => VirtualKey.O,
			Gdk.Key.p => VirtualKey.P,
			Gdk.Key.q => VirtualKey.Q,
			Gdk.Key.r => VirtualKey.R,
			Gdk.Key.s => VirtualKey.S,
			Gdk.Key.t => VirtualKey.T,
			Gdk.Key.u => VirtualKey.U,
			Gdk.Key.v => VirtualKey.V,
			Gdk.Key.w => VirtualKey.W,
			Gdk.Key.x => VirtualKey.X,
			Gdk.Key.y => VirtualKey.Y,
			Gdk.Key.z => VirtualKey.Z,
			// Gdk.Key.LeftWindows => VirtualKey.LeftWindows,
			// Gdk.Key.RightWindows => VirtualKey.RightWindows,
			// Gdk.Key.Application => VirtualKey.Application,
			// Gdk.Key.Sleep => VirtualKey.Sleep,
			Gdk.Key.KP_0 => VirtualKey.NumberPad0,
			Gdk.Key.KP_1 => VirtualKey.NumberPad1,
			Gdk.Key.KP_2 => VirtualKey.NumberPad2,
			Gdk.Key.KP_3 => VirtualKey.NumberPad3,
			Gdk.Key.KP_4 => VirtualKey.NumberPad4,
			Gdk.Key.KP_5 => VirtualKey.NumberPad5,
			Gdk.Key.KP_6 => VirtualKey.NumberPad6,
			Gdk.Key.KP_7 => VirtualKey.NumberPad7,
			Gdk.Key.KP_8 => VirtualKey.NumberPad8,
			Gdk.Key.KP_9 => VirtualKey.NumberPad9,

			Gdk.Key.plus => VirtualKey.Add,
			Gdk.Key.minus => VirtualKey.Subtract,
			Gdk.Key.multiply => VirtualKey.Multiply,
			Gdk.Key.slash => VirtualKey.Divide,

			Gdk.Key.KP_Multiply => VirtualKey.Multiply,
			Gdk.Key.KP_Add => VirtualKey.Add,
			Gdk.Key.KP_Separator => VirtualKey.Separator,
			Gdk.Key.KP_Subtract => VirtualKey.Subtract,
			Gdk.Key.KP_Decimal => VirtualKey.Decimal,
			Gdk.Key.KP_Divide => VirtualKey.Divide,
			Gdk.Key.F1 => VirtualKey.F1,
			Gdk.Key.F2 => VirtualKey.F2,
			Gdk.Key.F3 => VirtualKey.F3,
			Gdk.Key.F4 => VirtualKey.F4,
			Gdk.Key.F5 => VirtualKey.F5,
			Gdk.Key.F6 => VirtualKey.F6,
			Gdk.Key.F7 => VirtualKey.F7,
			Gdk.Key.F8 => VirtualKey.F8,
			Gdk.Key.F9 => VirtualKey.F9,
			Gdk.Key.F10 => VirtualKey.F10,
			Gdk.Key.F11 => VirtualKey.F11,
			Gdk.Key.F12 => VirtualKey.F12,
			Gdk.Key.F13 => VirtualKey.F13,
			Gdk.Key.F14 => VirtualKey.F14,
			Gdk.Key.F15 => VirtualKey.F15,
			Gdk.Key.F16 => VirtualKey.F16,
			Gdk.Key.F17 => VirtualKey.F17,
			Gdk.Key.F18 => VirtualKey.F18,
			Gdk.Key.F19 => VirtualKey.F19,
			Gdk.Key.F20 => VirtualKey.F20,
			Gdk.Key.F21 => VirtualKey.F21,
			Gdk.Key.F22 => VirtualKey.F22,
			Gdk.Key.F23 => VirtualKey.F23,
			Gdk.Key.F24 => VirtualKey.F24,
			// Gdk.Key.NavigationView => VirtualKey.NavigationView,
			// Gdk.Key.NavigationMenu => VirtualKey.NavigationMenu,
			// Gdk.Key.NavigationUp => VirtualKey.NavigationUp,
			// Gdk.Key.NavigationDown => VirtualKey.NavigationDown,
			// Gdk.Key.NavigationLeft => VirtualKey.NavigationLeft,
			// Gdk.Key.NavigationRight => VirtualKey.NavigationRight,
			// Gdk.Key.NavigationAccept => VirtualKey.NavigationAccept,
			// Gdk.Key.NavigationCancel => VirtualKey.NavigationCancel,
			Gdk.Key.Num_Lock => VirtualKey.NumberKeyLock,
			Gdk.Key.Scroll_Lock => VirtualKey.Scroll,
			Gdk.Key.Shift_L => VirtualKey.LeftShift,
			Gdk.Key.Shift_R => VirtualKey.RightShift,
			Gdk.Key.Control_L => VirtualKey.LeftControl,
			Gdk.Key.Control_R => VirtualKey.RightControl,
			Gdk.Key.Alt_L => VirtualKey.LeftMenu,
			Gdk.Key.Alt_R => VirtualKey.RightMenu,
			// Gdk.Key.GoBack => VirtualKey.GoBack,
			// Gdk.Key.GoForward => VirtualKey.GoForward,
			// Gdk.Key.Refresh => VirtualKey.Refresh,
			// Gdk.Key.Stop => VirtualKey.Stop,
			// Gdk.Key.Search => VirtualKey.Search,
			// Gdk.Key.Favorites => VirtualKey.Favorites,
			// Gdk.Key.GoHome => VirtualKey.GoHome,
			// Gdk.Key.GamepadA => VirtualKey.GamepadA,
			// Gdk.Key.GamepadB => VirtualKey.GamepadB,
			// Gdk.Key.GamepadX => VirtualKey.GamepadX,
			// Gdk.Key.GamepadY => VirtualKey.GamepadY,
			// Gdk.Key.GamepadRightShoulder => VirtualKey.GamepadRightShoulder,
			// Gdk.Key.GamepadLeftShoulder => VirtualKey.GamepadLeftShoulder,
			// Gdk.Key.GamepadLeftTrigger => VirtualKey.GamepadLeftTrigger,
			// Gdk.Key.GamepadRightTrigger => VirtualKey.GamepadRightTrigger,
			// Gdk.Key.GamepadDPadUp => VirtualKey.GamepadDPadUp,
			// Gdk.Key.GamepadDPadDown => VirtualKey.GamepadDPadDown,
			// Gdk.Key.GamepadDPadLeft => VirtualKey.GamepadDPadLeft,
			// Gdk.Key.GamepadDPadRight => VirtualKey.GamepadDPadRight,
			// Gdk.Key.GamepadMenu => VirtualKey.GamepadMenu,
			// Gdk.Key.GamepadView => VirtualKey.GamepadView,
			// Gdk.Key.GamepadLeftThumbstickButton => VirtualKey.GamepadLeftThumbstickButton,
			// Gdk.Key.GamepadRightThumbstickButton => VirtualKey.GamepadRightThumbstickButton,
			// Gdk.Key.GamepadLeftThumbstickUp => VirtualKey.GamepadLeftThumbstickUp,
			// Gdk.Key.GamepadLeftThumbstickDown => VirtualKey.GamepadLeftThumbstickDown,
			// Gdk.Key.GamepadLeftThumbstickRight => VirtualKey.GamepadLeftThumbstickRight,
			// Gdk.Key.GamepadLeftThumbstickLeft => VirtualKey.GamepadLeftThumbstickLeft,
			// Gdk.Key.GamepadRightThumbstickUp => VirtualKey.GamepadRightThumbstickUp,
			// Gdk.Key.GamepadRightThumbstickDown => VirtualKey.GamepadRightThumbstickDown,
			// Gdk.Key.GamepadRightThumbstickRight => VirtualKey.GamepadRightThumbstickRight,
			// Gdk.Key.GamepadRightThumbstickLeft => 218= GamepadRightThumbstickLeft

			_ => VirtualKey.None,
		};
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
