using System;
using System.Collections.Generic;
using System.Text;
using Android.Views;
using Uno.Extensions;
using Uno.Logging;
using Windows.System;
using Microsoft.Extensions.Logging;

namespace Uno.UI.Extensions
{
	public static class KeycodeExtensions
	{
		public static VirtualKey ToVirtualKey(this Keycode keycode)
		{
			switch (keycode)
			{
				case Keycode.Unknown:
					return VirtualKey.None;
				//case Keycode.LeftButton:
				//    return VirtualKey.LeftButton;
				//case Keycode.RightButton:
				//    return VirtualKey.RightButton;
				//case Keycode.Cancel:
				//    return VirtualKey.Cancel;
				//case Keycode.MiddleButton:
				//    return VirtualKey.MiddleButton;
				//case Keycode.XButton1:
				//    return VirtualKey.XButton1;
				//case Keycode.XButton2:
				//    return VirtualKey.XButton2;
				case Keycode.Back:
					return VirtualKey.Back;
				case Keycode.Tab:
					return VirtualKey.Tab;
				case Keycode.Clear:
					return VirtualKey.Clear;
				case Keycode.Enter:
					return VirtualKey.Enter;
				//case Keycode.Shift:
				//    return VirtualKey.Shift;
				//case Keycode.Control:
				//    return VirtualKey.Control;
				case Keycode.Menu:
					return VirtualKey.Menu;
				case Keycode.MediaPause:
				case Keycode.MediaPlayPause:
					return VirtualKey.Pause;
				case Keycode.CapsLock:
					return VirtualKey.CapitalLock;
#if __ANDROID_16__
				case Keycode.Kana:
					return VirtualKey.Kana;
#endif
				//case Keycode.Hangul:
				//    return VirtualKey.Hangul;
				//case Keycode.Junja:
				//    return VirtualKey.Junja;
				//case Keycode.Final:
				//    return VirtualKey.Final;
				//case Keycode.Hanja:
				//    return VirtualKey.Hanja;
				//case Keycode.Kanji:
				//    return VirtualKey.Kanji;
				case Keycode.Escape:
					return VirtualKey.Escape;
				//case Keycode.Convert:
				//    return VirtualKey.Convert;
				//case Keycode.NonConvert:
				//    return VirtualKey.NonConvert;
				//case Keycode.Accept:
				//    return VirtualKey.Accept;
				//case Keycode.ModeChange:
				//    return VirtualKey.ModeChange;
				case Keycode.Space:
					return VirtualKey.Space;
				case Keycode.PageUp:
					return VirtualKey.PageUp;
				case Keycode.PageDown:
					return VirtualKey.PageDown;
				case Keycode.MoveEnd:
					return VirtualKey.End;
				case Keycode.Home:
					return VirtualKey.Home;
				case Keycode.DpadLeft:
					return VirtualKey.Left;
				case Keycode.DpadUp:
					return VirtualKey.Up;
				case Keycode.DpadRight:
					return VirtualKey.Right;
				case Keycode.DpadDown:
					return VirtualKey.Down;
				//case Keycode.Select:
				//    return VirtualKey.Select;
				//case Keycode.Print:
				//    return VirtualKey.Print;
				//case Keycode.Execute:
				//    return VirtualKey.Execute;
				//case Keycode.Snapshot:
				//    return VirtualKey.Snapshot;
				case Keycode.Insert:
					return VirtualKey.Insert;
				case Keycode.Del:
					return VirtualKey.Delete;
#if __ANDROID_21__
				case Keycode.Help:
					return VirtualKey.Help;
#endif
				case Keycode.Num0:
					return VirtualKey.Number0;
				case Keycode.Num1:
					return VirtualKey.Number1;
				case Keycode.Num2:
					return VirtualKey.Number2;
				case Keycode.Num3:
					return VirtualKey.Number3;
				case Keycode.Num4:
					return VirtualKey.Number4;
				case Keycode.Num5:
					return VirtualKey.Number5;
				case Keycode.Num6:
					return VirtualKey.Number6;
				case Keycode.Num7:
					return VirtualKey.Number7;
				case Keycode.Num8:
					return VirtualKey.Number8;
				case Keycode.Num9:
					return VirtualKey.Number9;
				case Keycode.A:
					return VirtualKey.A;
				case Keycode.B:
					return VirtualKey.B;
				case Keycode.C:
					return VirtualKey.C;
				case Keycode.D:
					return VirtualKey.D;
				case Keycode.E:
					return VirtualKey.E;
				case Keycode.F:
					return VirtualKey.F;
				case Keycode.G:
					return VirtualKey.G;
				case Keycode.H:
					return VirtualKey.H;
				case Keycode.I:
					return VirtualKey.I;
				case Keycode.J:
					return VirtualKey.J;
				case Keycode.K:
					return VirtualKey.K;
				case Keycode.L:
					return VirtualKey.L;
				case Keycode.M:
					return VirtualKey.M;
				case Keycode.N:
					return VirtualKey.N;
				case Keycode.O:
					return VirtualKey.O;
				case Keycode.P:
					return VirtualKey.P;
				case Keycode.Q:
					return VirtualKey.Q;
				case Keycode.R:
					return VirtualKey.R;
				case Keycode.S:
					return VirtualKey.S;
				case Keycode.T:
					return VirtualKey.T;
				case Keycode.U:
					return VirtualKey.U;
				case Keycode.V:
					return VirtualKey.V;
				case Keycode.W:
					return VirtualKey.W;
				case Keycode.X:
					return VirtualKey.X;
				case Keycode.Y:
					return VirtualKey.Y;
				case Keycode.Z:
					return VirtualKey.Z;
				//case Keycode.LeftWindows:
				//    return VirtualKey.LeftWindows;
				//case Keycode.RightWindows:
				//    return VirtualKey.RightWindows;
				case Keycode.AppSwitch:
					return VirtualKey.Application;
#if __ANDROID_20__
				case Keycode.Sleep:
					return VirtualKey.Sleep;
#endif
				case Keycode.Numpad0:
					return VirtualKey.NumberPad0;
				case Keycode.Numpad1:
					return VirtualKey.NumberPad1;
				case Keycode.Numpad2:
					return VirtualKey.NumberPad2;
				case Keycode.Numpad3:
					return VirtualKey.NumberPad3;
				case Keycode.Numpad4:
					return VirtualKey.NumberPad4;
				case Keycode.Numpad5:
					return VirtualKey.NumberPad5;
				case Keycode.Numpad6:
					return VirtualKey.NumberPad6;
				case Keycode.Numpad7:
					return VirtualKey.NumberPad7;
				case Keycode.Numpad8:
					return VirtualKey.NumberPad8;
				case Keycode.Numpad9:
					return VirtualKey.NumberPad9;
				case Keycode.NumpadMultiply:
					return VirtualKey.Multiply;
				case Keycode.NumpadAdd:
					return VirtualKey.Add;
				case Keycode.NumpadComma:
					return VirtualKey.Separator;
				case Keycode.NumpadSubtract:
					return VirtualKey.Subtract;
				case Keycode.NumpadDot:
					return VirtualKey.Decimal;
				case Keycode.NumpadDivide:
					return VirtualKey.Divide;
				case Keycode.F1:
					return VirtualKey.F1;
				case Keycode.F2:
					return VirtualKey.F2;
				case Keycode.F3:
					return VirtualKey.F3;
				case Keycode.F4:
					return VirtualKey.F4;
				case Keycode.F5:
					return VirtualKey.F5;
				case Keycode.F6:
					return VirtualKey.F6;
				case Keycode.F7:
					return VirtualKey.F7;
				case Keycode.F8:
					return VirtualKey.F8;
				case Keycode.F9:
					return VirtualKey.F9;
				case Keycode.F10:
					return VirtualKey.F10;
				case Keycode.F11:
					return VirtualKey.F11;
				case Keycode.F12:
					return VirtualKey.F12;
				//case Keycode.F13:
				//    return VirtualKey.F13;
				//case Keycode.F14:
				//    return VirtualKey.F14;
				//case Keycode.F15:
				//    return VirtualKey.F15;
				//case Keycode.F16:
				//    return VirtualKey.F16;
				//case Keycode.F17:
				//    return VirtualKey.F17;
				//case Keycode.F18:
				//    return VirtualKey.F18;
				//case Keycode.F19:
				//    return VirtualKey.F19;
				//case Keycode.F20:
				//    return VirtualKey.F20;
				//case Keycode.F21:
				//    return VirtualKey.F21;
				//case Keycode.F22:
				//    return VirtualKey.F22;
				//case Keycode.F23:
				//    return VirtualKey.F23;
				//case Keycode.F24:
				//    return VirtualKey.F24;
				case Keycode.NumLock:
					return VirtualKey.NumberKeyLock;
				case Keycode.ScrollLock:
					return VirtualKey.Scroll;
				case Keycode.ShiftLeft:
					return VirtualKey.LeftShift;
				case Keycode.ShiftRight:
					return VirtualKey.RightShift;
				case Keycode.CtrlLeft:
					return VirtualKey.LeftControl;
				case Keycode.CtrlRight:
					return VirtualKey.RightControl;
				//case Keycode.LeftMenu:
				//    return VirtualKey.LeftMenu;
				//case Keycode.RightMenu:
				//    return VirtualKey.RightMenu;
				//case Keycode.GoBack:
				//    return VirtualKey.GoBack;
				//case Keycode.GoForward:
				//    return VirtualKey.GoForward;
				//case Keycode.Refresh:
				//    return VirtualKey.Refresh;
				case Keycode.MediaStop:
					return VirtualKey.Stop;
				case Keycode.Search:
					return VirtualKey.Search;
				//case Keycode.Favorites:
				//    return VirtualKey.Favorites;
				//case Keycode.GoHome:
				//    return VirtualKey.GoHome;
				default:
					{
						if(keycode.Log().IsEnabled(LogLevel.Warning))
						{
							keycode.Log().Warn($"Key pressed but not mapped : " + keycode);
						}
						return VirtualKey.None;
					}
			}
		}
	}
}
