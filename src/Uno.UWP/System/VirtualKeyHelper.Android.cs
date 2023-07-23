// Some mappings based on
// https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
// https://lists.w3.org/Archives/Public/www-dom/2010JulSep/att-0182/keyCode-spec.html

using Android.Views;
using Uno.Foundation.Logging;
using Windows.UI.Core;

namespace Windows.System;

internal static class VirtualKeyHelper
{
	public static VirtualKey FromKeyCode(Keycode key)
	{
		switch (key)
		{
			case Keycode.Num0: return VirtualKey.Number0;
			case Keycode.Num1: return VirtualKey.Number1;
			case Keycode.Num2: return VirtualKey.Number2;
			case Keycode.Num3: return VirtualKey.Number3;
			case Keycode.Num4: return VirtualKey.Number4;
			case Keycode.Num5: return VirtualKey.Number5;
			case Keycode.Num6: return VirtualKey.Number6;
			case Keycode.Num7: return VirtualKey.Number7;
			case Keycode.Num8: return VirtualKey.Number8;
			case Keycode.Num9: return VirtualKey.Number9;

			case Keycode.Numpad0: return VirtualKey.NumberPad0;
			case Keycode.Numpad1: return VirtualKey.NumberPad1;
			case Keycode.Numpad2: return VirtualKey.NumberPad2;
			case Keycode.Numpad3: return VirtualKey.NumberPad3;
			case Keycode.Numpad4: return VirtualKey.NumberPad4;
			case Keycode.Numpad5: return VirtualKey.NumberPad5;
			case Keycode.Numpad6: return VirtualKey.NumberPad6;
			case Keycode.Numpad7: return VirtualKey.NumberPad7;
			case Keycode.Numpad8: return VirtualKey.NumberPad8;
			case Keycode.Numpad9: return VirtualKey.NumberPad9;

			case Keycode.A: return VirtualKey.A;
			case Keycode.B: return VirtualKey.B;
			case Keycode.C: return VirtualKey.C;
			case Keycode.D: return VirtualKey.D;
			case Keycode.E: return VirtualKey.E;
			case Keycode.F: return VirtualKey.F;
			case Keycode.G: return VirtualKey.G;
			case Keycode.H: return VirtualKey.H;
			case Keycode.I: return VirtualKey.I;
			case Keycode.J: return VirtualKey.J;
			case Keycode.K: return VirtualKey.K;
			case Keycode.L: return VirtualKey.L;
			case Keycode.M: return VirtualKey.M;
			case Keycode.N: return VirtualKey.N;
			case Keycode.O: return VirtualKey.O;
			case Keycode.P: return VirtualKey.P;
			case Keycode.Q: return VirtualKey.Q;
			case Keycode.R: return VirtualKey.R;
			case Keycode.S: return VirtualKey.S;
			case Keycode.T: return VirtualKey.T;
			case Keycode.U: return VirtualKey.U;
			case Keycode.V: return VirtualKey.V;
			case Keycode.W: return VirtualKey.W;
			case Keycode.X: return VirtualKey.X;
			case Keycode.Y: return VirtualKey.Y;
			case Keycode.Z: return VirtualKey.Z;

			case Keycode.Comma: return (VirtualKey)188;
			case Keycode.Period: return (VirtualKey)190;
			case Keycode.Equals or Keycode.NumpadEquals: return (VirtualKey)187;
			case Keycode.NumpadDot or Keycode.NumpadComma: return VirtualKey.Decimal;
			case Keycode.NumpadDivide: return VirtualKey.Divide;
			case Keycode.NumpadSubtract: return VirtualKey.Subtract;

			case Keycode.NumpadMultiply: return VirtualKey.Multiply;
			case Keycode.NumpadAdd: return VirtualKey.Add;
			case Keycode.Enter: return VirtualKey.Enter;
			case Keycode.NumpadEnter: return VirtualKey.Enter;
			case Keycode.Clear: return VirtualKey.Clear;

			case Keycode.Space: return VirtualKey.Space;
			case Keycode.Tab: return VirtualKey.Tab;
			case Keycode.Del: return VirtualKey.Back;
			case Keycode.ForwardDel: return VirtualKey.Delete;

			// Modifiers
			case Keycode.Escape: return VirtualKey.Escape;
			case Keycode.MetaLeft: return VirtualKey.LeftWindows;

			// Functions
			case Keycode.F1: return VirtualKey.F1;
			case Keycode.F2: return VirtualKey.F2;
			case Keycode.F3: return VirtualKey.F3;
			case Keycode.F4: return VirtualKey.F4;
			case Keycode.F5: return VirtualKey.F5;
			case Keycode.F6: return VirtualKey.F6;
			case Keycode.F7: return VirtualKey.F7;
			case Keycode.F8: return VirtualKey.F8;
			case Keycode.F9: return VirtualKey.F9;
			case Keycode.F10: return VirtualKey.F10;
			case Keycode.F11: return VirtualKey.F11;
			case Keycode.F12: return VirtualKey.F12;

			case Keycode.Semicolon: return (VirtualKey)186;

			// Navigation
			case Keycode.Help: return VirtualKey.Help;
			case Keycode.NumLock: return VirtualKey.NumberKeyLock;
			case Keycode.ScrollLock: return VirtualKey.Scroll;

			case Keycode.Search: return VirtualKey.Search;
			case Keycode.Insert: return VirtualKey.Insert;
			case Keycode.MoveHome: return VirtualKey.Home;
			case Keycode.MoveEnd: return VirtualKey.End;
			case Keycode.PageUp: return VirtualKey.PageUp;
			case Keycode.PageDown: return VirtualKey.PageDown;
			case Keycode.AppSwitch: return VirtualKey.Application;

			case Keycode.CtrlLeft: return VirtualKey.LeftControl;
			case Keycode.CtrlRight: return VirtualKey.RightControl;
			case Keycode.ShiftLeft: return VirtualKey.LeftShift;
			case Keycode.ShiftRight: return VirtualKey.RightShift;

			case Keycode.SystemNavigationUp: return VirtualKey.Up;
			case Keycode.SystemNavigationDown: return VirtualKey.Down;
			case Keycode.SystemNavigationLeft: return VirtualKey.Left;
			case Keycode.SystemNavigationRight: return VirtualKey.Right;

			case Keycode.DpadUp: return VirtualKey.GamepadDPadUp;
			case Keycode.DpadDown: return VirtualKey.GamepadDPadDown;
			case Keycode.DpadLeft: return VirtualKey.GamepadDPadLeft;
			case Keycode.DpadRight: return VirtualKey.GamepadDPadRight;
			case Keycode.DpadCenter: return VirtualKey.GamepadA;

			// Android TV Remote
			case Keycode.Bookmark: return VirtualKey.Favorites;
			case Keycode.Menu: return VirtualKey.Menu;
			case Keycode.Back: return VirtualKey.GoBack;
			case Keycode.Home: return VirtualKey.GoHome;
			case Keycode.Forward: return VirtualKey.GoForward;

			case Keycode.VolumeMute: return (VirtualKey)173;
			case Keycode.VolumeDown: return (VirtualKey)174;
			case Keycode.VolumeUp: return (VirtualKey)175;
			case Keycode.MediaNext: return (VirtualKey)176;
			case Keycode.MediaPrevious: return (VirtualKey)177;
			case Keycode.MediaStop: return (VirtualKey)178;
			case Keycode.MediaPlayPause: return (VirtualKey)179;
			case Keycode.MediaPause: return (VirtualKey)179;
			case Keycode.MediaPlay: return (VirtualKey)250;

			case Keycode.CapsLock: return VirtualKey.CapitalLock;
			case Keycode.Kana: return VirtualKey.Kana;
			case Keycode.Sleep: return VirtualKey.Sleep;
			default:
				{
					if (typeof(CoreWindow).Log().IsEnabled(LogLevel.Warning))
					{
						typeof(CoreWindow).Log().Warn($"Key pressed but not mapped : " + key);
					}

					return VirtualKey.None;
				}
		}
	}
}
