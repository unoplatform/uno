#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElmSharp;
using Windows.Devices.Input;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.Logging;
using Microsoft.Extensions.Logging;
using TizenWindow = ElmSharp.Window;
using Windows.System;
using System.Threading;
using SkiaSharp.Views.Tizen;
using Windows.Graphics.Display;
using Windows.Foundation;
using Tizen.NUI;
using Tizen.Uix.InputMethod;

namespace Uno.UI.Runtime.Skia
{
	partial class TizenCoreWindowExtension : ICoreWindowExtension
	{
		private void OnKeyDown(object sender, EvasKeyEventArgs args)
		{
			var (virtualKey, keyCode) = ParseKey(args);

			try
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"OnKeyDown: ({args.KeyName}/{keyCode}) -> {virtualKey}");
				}

				_ownerEvents.RaiseKeyDown(
					new KeyEventArgs(
						"keyboard",
						virtualKey,
						new CorePhysicalKeyStatus
						{
							ScanCode = (uint)keyCode,
							RepeatCount = 1,
						}));
			}
			catch (Exception e)
			{
				Windows.UI.Xaml.Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}

		private void OnKeyUp(object sender, EvasKeyEventArgs args)
		{
			var (virtualKey, keyCode) = ParseKey(args);

			try
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"OnKeyUp: ({args.KeyName}/{keyCode}) -> {virtualKey}");
				}

				_ownerEvents.RaiseKeyUp(
					new KeyEventArgs(
						"keyboard",
						virtualKey,
						new CorePhysicalKeyStatus
						{
							ScanCode = (uint)keyCode,
							RepeatCount = 1,
						}));
			}
			catch (Exception e)
			{
				Windows.UI.Xaml.Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}

		private (VirtualKey, KeyCode) ParseKey(EvasKeyEventArgs e)
		{
			if (Enum.TryParse<KeyCode>(e.KeyName, true, out var keyCode))
			{
				return (ConvertKey(keyCode), keyCode);
			}
			else
			{
				return (VirtualKey.None, (KeyCode)0);
			}
		}

		private VirtualKey ConvertKey(object keyCode)
		{
			// In this function, commented out lines correspond to VirtualKeys not yet
			// mapped to their native counterparts. Uncomment and fix as needed.

			return keyCode switch
			{
				// KeyCode.LeftButton => VirtualKey.LeftButton,
				// KeyCode.RightButton => VirtualKey.RightButton,
				KeyCode.Cancel => VirtualKey.Cancel,
				// KeyCode.MiddleButton => VirtualKey.MiddleButton,
				// KeyCode.XButton1 => VirtualKey.XButton1,
				// KeyCode.XButton2 => VirtualKey.XButton2,
				KeyCode.BackSpace => VirtualKey.Back,
				KeyCode.Tab => VirtualKey.Tab,
				KeyCode.Clear => VirtualKey.Clear,
				KeyCode.Return => VirtualKey.Enter,
				// KeyCode.Shift => VirtualKey.Shift,
				// KeyCode.Control => VirtualKey.Control,
				KeyCode.Menu => VirtualKey.Menu,
				KeyCode.Pause => VirtualKey.Pause,
				KeyCode.CapsLock => VirtualKey.CapitalLock,
				// KeyCode.Kana => VirtualKey.Kana,
				// KeyCode.Hangul => VirtualKey.Hangul,
				// KeyCode.Junja => VirtualKey.Junja,
				// KeyCode.Final => VirtualKey.Final,
				// KeyCode.Hanja => VirtualKey.Hanja,
				// KeyCode.Kanji => VirtualKey.Kanji,
				KeyCode.Escape => VirtualKey.Escape,
				// KeyCode.Convert => VirtualKey.Convert,
				// KeyCode.NonConvert => VirtualKey.NonConvert,
				// KeyCode.Accept => VirtualKey.Accept,
				// KeyCode.ModeChange => VirtualKey.ModeChange,
				KeyCode.Space => VirtualKey.Space,
				KeyCode.Page_Up => VirtualKey.PageUp,
				KeyCode.Page_Down => VirtualKey.PageDown,
				KeyCode.End => VirtualKey.End,
				KeyCode.Home => VirtualKey.Home,
				KeyCode.Left => VirtualKey.Left,
				KeyCode.Up => VirtualKey.Up,
				KeyCode.Right => VirtualKey.Right,
				KeyCode.Down => VirtualKey.Down,
				KeyCode.Select => VirtualKey.Select,
				KeyCode.Print => VirtualKey.Print,
				KeyCode.Execute => VirtualKey.Execute,
				// KeyCode.Print => VirtualKey.Snapshot,
				KeyCode.Insert => VirtualKey.Insert,
				KeyCode.Delete => VirtualKey.Delete,
				KeyCode.Help => VirtualKey.Help,
				KeyCode.Keypad0 => VirtualKey.Number0,
				KeyCode.Keypad1 => VirtualKey.Number1,
				KeyCode.Keypad2 => VirtualKey.Number2,
				KeyCode.Keypad3 => VirtualKey.Number3,
				KeyCode.Keypad4 => VirtualKey.Number4,
				KeyCode.Keypad5 => VirtualKey.Number5,
				KeyCode.Keypad6 => VirtualKey.Number6,
				KeyCode.Keypad7 => VirtualKey.Number7,
				KeyCode.Keypad8 => VirtualKey.Number8,
				KeyCode.Keypad9 => VirtualKey.Number9,
				KeyCode.KeypadA => VirtualKey.A,
				KeyCode.KeypadB => VirtualKey.B,
				KeyCode.KeypadC => VirtualKey.C,
				KeyCode.KeypadD => VirtualKey.D,
				KeyCode.KeypadE => VirtualKey.E,
				KeyCode.KeypadF => VirtualKey.F,
				KeyCode.KeypadG => VirtualKey.G,
				KeyCode.KeypadH => VirtualKey.H,
				KeyCode.KeypadI => VirtualKey.I,
				KeyCode.KeypadJ => VirtualKey.J,
				KeyCode.KeypadK => VirtualKey.K,
				KeyCode.KeypadL => VirtualKey.L,
				KeyCode.KeypadM => VirtualKey.M,
				KeyCode.KeypadN => VirtualKey.N,
				KeyCode.KeypadO => VirtualKey.O,
				KeyCode.KeypadP => VirtualKey.P,
				KeyCode.KeypadQ => VirtualKey.Q,
				KeyCode.KeypadR => VirtualKey.R,
				KeyCode.KeypadS => VirtualKey.S,
				KeyCode.KeypadT => VirtualKey.T,
				KeyCode.KeypadU => VirtualKey.U,
				KeyCode.KeypadV => VirtualKey.V,
				KeyCode.KeypadW => VirtualKey.W,
				KeyCode.KeypadX => VirtualKey.X,
				KeyCode.KeypadY => VirtualKey.Y,
				KeyCode.KeypadZ => VirtualKey.Z,
				// KeyCode.LeftWindows => VirtualKey.LeftWindows,
				// KeyCode.RightWindows => VirtualKey.RightWindows,
				// KeyCode.Application => VirtualKey.Application,
				// KeyCode.Sleep => VirtualKey.Sleep,
				KeyCode.KP0 => VirtualKey.NumberPad0,
				KeyCode.KP1 => VirtualKey.NumberPad1,
				KeyCode.KP2 => VirtualKey.NumberPad2,
				KeyCode.KP3 => VirtualKey.NumberPad3,
				KeyCode.KP4 => VirtualKey.NumberPad4,
				KeyCode.KP5 => VirtualKey.NumberPad5,
				KeyCode.KP6 => VirtualKey.NumberPad6,
				KeyCode.KP7 => VirtualKey.NumberPad7,
				KeyCode.KP8 => VirtualKey.NumberPad8,
				KeyCode.KP9 => VirtualKey.NumberPad9,
				KeyCode.KPMultiply => VirtualKey.Multiply,
				KeyCode.KPAdd => VirtualKey.Add,
				KeyCode.KPSeparator => VirtualKey.Separator,
				KeyCode.KPSubtract => VirtualKey.Subtract,
				KeyCode.KPDecimal => VirtualKey.Decimal,
				KeyCode.KPDivide => VirtualKey.Divide,
				KeyCode.F1 => VirtualKey.F1,
				KeyCode.F2 => VirtualKey.F2,
				KeyCode.F3 => VirtualKey.F3,
				KeyCode.F4 => VirtualKey.F4,
				KeyCode.F5 => VirtualKey.F5,
				KeyCode.F6 => VirtualKey.F6,
				KeyCode.F7 => VirtualKey.F7,
				KeyCode.F8 => VirtualKey.F8,
				KeyCode.F9 => VirtualKey.F9,
				KeyCode.F10 => VirtualKey.F10,
				KeyCode.F11 => VirtualKey.F11,
				KeyCode.F12 => VirtualKey.F12,
				KeyCode.F13 => VirtualKey.F13,
				KeyCode.F14 => VirtualKey.F14,
				KeyCode.F15 => VirtualKey.F15,
				KeyCode.F16 => VirtualKey.F16,
				KeyCode.F17 => VirtualKey.F17,
				KeyCode.F18 => VirtualKey.F18,
				KeyCode.F19 => VirtualKey.F19,
				KeyCode.F20 => VirtualKey.F20,
				KeyCode.F21 => VirtualKey.F21,
				KeyCode.F22 => VirtualKey.F22,
				KeyCode.F23 => VirtualKey.F23,
				KeyCode.F24 => VirtualKey.F24,
				// KeyCode.NavigationView => VirtualKey.NavigationView,
				// KeyCode.NavigationMenu => VirtualKey.NavigationMenu,
				// KeyCode.NavigationUp => VirtualKey.NavigationUp,
				// KeyCode.NavigationDown => VirtualKey.NavigationDown,
				// KeyCode.NavigationLeft => VirtualKey.NavigationLeft,
				// KeyCode.NavigationRight => VirtualKey.NavigationRight,
				// KeyCode.NavigationAccept => VirtualKey.NavigationAccept,
				// KeyCode.NavigationCancel => VirtualKey.NavigationCancel,
				KeyCode.Num_Lock => VirtualKey.NumberKeyLock,
				KeyCode.ScrollLock => VirtualKey.Scroll,
				KeyCode.ShiftL => VirtualKey.LeftShift,
				KeyCode.ShiftR	 => VirtualKey.RightShift,
				KeyCode.ControlL => VirtualKey.LeftControl,
				KeyCode.ControlR => VirtualKey.RightControl,
				KeyCode.AltL => VirtualKey.LeftMenu,
				KeyCode.AltR => VirtualKey.RightMenu,
				// KeyCode.GoBack => VirtualKey.GoBack,
				// KeyCode.GoForward => VirtualKey.GoForward,
				// KeyCode.Refresh => VirtualKey.Refresh,
				// KeyCode.Stop => VirtualKey.Stop,
				// KeyCode.Search => VirtualKey.Search,
				// KeyCode.Favorites => VirtualKey.Favorites,
				// KeyCode.GoHome => VirtualKey.GoHome,
				// KeyCode.GamepadA => VirtualKey.GamepadA,
				// KeyCode.GamepadB => VirtualKey.GamepadB,
				// KeyCode.GamepadX => VirtualKey.GamepadX,
				// KeyCode.GamepadY => VirtualKey.GamepadY,
				// KeyCode.GamepadRightShoulder => VirtualKey.GamepadRightShoulder,
				// KeyCode.GamepadLeftShoulder => VirtualKey.GamepadLeftShoulder,
				// KeyCode.GamepadLeftTrigger => VirtualKey.GamepadLeftTrigger,
				// KeyCode.GamepadRightTrigger => VirtualKey.GamepadRightTrigger,
				// KeyCode.GamepadDPadUp => VirtualKey.GamepadDPadUp,
				// KeyCode.GamepadDPadDown => VirtualKey.GamepadDPadDown,
				// KeyCode.GamepadDPadLeft => VirtualKey.GamepadDPadLeft,
				// KeyCode.GamepadDPadRight => VirtualKey.GamepadDPadRight,
				// KeyCode.GamepadMenu => VirtualKey.GamepadMenu,
				// KeyCode.GamepadView => VirtualKey.GamepadView,
				// KeyCode.GamepadLeftThumbstickButton => VirtualKey.GamepadLeftThumbstickButton,
				// KeyCode.GamepadRightThumbstickButton => VirtualKey.GamepadRightThumbstickButton,
				// KeyCode.GamepadLeftThumbstickUp => VirtualKey.GamepadLeftThumbstickUp,
				// KeyCode.GamepadLeftThumbstickDown => VirtualKey.GamepadLeftThumbstickDown,
				// KeyCode.GamepadLeftThumbstickRight => VirtualKey.GamepadLeftThumbstickRight,
				// KeyCode.GamepadLeftThumbstickLeft => VirtualKey.GamepadLeftThumbstickLeft,
				// KeyCode.GamepadRightThumbstickUp => VirtualKey.GamepadRightThumbstickUp,
				// KeyCode.GamepadRightThumbstickDown => VirtualKey.GamepadRightThumbstickDown,
				// KeyCode.GamepadRightThumbstickRight => VirtualKey.GamepadRightThumbstickRight,
				// KeyCode.GamepadRightThumbstickLeft => VirtualKey.GamepadRightThumbstickLeft,

				_ => VirtualKey.None,
			};
		}
	}
}
