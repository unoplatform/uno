using Uno.Extensions;
using Windows.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.System;

using Uno.Foundation.Logging;

namespace Uno.UI.Extensions
{
	public static class CharacterExtensions
	{
		public static VirtualKey ToVirtualKey(this char thisChar)
		{
			switch (thisChar)
			{
				case '\0':
					return VirtualKey.None;
				//case .LeftButton:
				//    return VirtualKey.LeftButton;
				//case .RightButton:
				//    return VirtualKey.RightButton;
				//case .Cancel:
				//    return VirtualKey.Cancel;
				//case .MiddleButton:
				//    return VirtualKey.MiddleButton;
				//case .XButton1:
				//    return VirtualKey.XButton1;
				//case .XButton2:
				//    return VirtualKey.XButton2;
				//case .Back:
				//    return VirtualKey.Back;
				//case .Tab:
				//    return VirtualKey.Tab;
				//case .Clear:
				//    return VirtualKey.Clear;
				case '\n':
					return VirtualKey.Enter;
				//case .Shift:
				//    return VirtualKey.Shift;
				//case .Control:
				//    return VirtualKey.Control;
				//case .Menu:
				//    return VirtualKey.Menu;
				//case .MediaPause:
				//case .MediaPlayPause:
				//    return VirtualKey.Pause;
				//case .CapsLock:
				//    return VirtualKey.CapitalLock;
				//case .Kana:
				//    return VirtualKey.Kana;
				//case .Hangul:
				//    return VirtualKey.Hangul;
				//case .Junja:
				//    return VirtualKey.Junja;
				//case .Final:
				//    return VirtualKey.Final;
				//case .Hanja:
				//    return VirtualKey.Hanja;
				//case .Kanji:
				//    return VirtualKey.Kanji;
				//case .Escape:
				//    return VirtualKey.Escape;
				//case .Convert:
				//    return VirtualKey.Convert;
				//case .NonConvert:
				//    return VirtualKey.NonConvert;
				//case .Accept:
				//    return VirtualKey.Accept;
				//case .ModeChange:
				//    return VirtualKey.ModeChange;
				//case .Space:
				//    return VirtualKey.Space;
				//case .PageUp:
				//    return VirtualKey.PageUp;
				//case .PageDown:
				//    return VirtualKey.PageDown;
				//case .MoveEnd:
				//    return VirtualKey.End;
				//case .Home:
				//    return VirtualKey.Home;
				//case .DpadLeft:
				//    return VirtualKey.Left;
				//case .DpadUp:
				//    return VirtualKey.Up;
				//case .DpadRight:
				//    return VirtualKey.Right;
				//case .DpadDown:
				//    return VirtualKey.Down;
				//case .Select:
				//    return VirtualKey.Select;
				//case .Print:
				//    return VirtualKey.Print;
				//case .Execute:
				//    return VirtualKey.Execute;
				//case .Snapshot:
				//    return VirtualKey.Snapshot;
				//case .Insert:
				//    return VirtualKey.Insert;
				//case .Del:
				//    return VirtualKey.Delete;
				//case .Help:
				//    return VirtualKey.Help;
				case '0':
					return VirtualKey.Number0;
				case '1':
					return VirtualKey.Number1;
				case '2':
					return VirtualKey.Number2;
				case '3':
					return VirtualKey.Number3;
				case '4':
					return VirtualKey.Number4;
				case '5':
					return VirtualKey.Number5;
				case '6':
					return VirtualKey.Number6;
				case '7':
					return VirtualKey.Number7;
				case '8':
					return VirtualKey.Number8;
				case '9':
					return VirtualKey.Number9;

				case 'a':
					return VirtualKey.A;
				case 'b':
					return VirtualKey.B;
				case 'c':
					return VirtualKey.C;
				case 'd':
					return VirtualKey.D;
				case 'e':
					return VirtualKey.E;
				case 'f':
					return VirtualKey.F;
				case 'g':
					return VirtualKey.G;
				case 'h':
					return VirtualKey.H;
				case 'i':
					return VirtualKey.I;
				case 'j':
					return VirtualKey.J;
				case 'k':
					return VirtualKey.K;
				case 'l':
					return VirtualKey.L;
				case 'm':
					return VirtualKey.M;
				case 'n':
					return VirtualKey.N;
				case 'o':
					return VirtualKey.O;
				case 'p':
					return VirtualKey.P;
				case 'q':
					return VirtualKey.Q;
				case 'r':
					return VirtualKey.R;
				case 's':
					return VirtualKey.S;
				case 't':
					return VirtualKey.T;
				case 'u':
					return VirtualKey.U;
				case 'v':
					return VirtualKey.V;
				case 'w':
					return VirtualKey.W;
				case 'x':
					return VirtualKey.X;
				case 'y':
					return VirtualKey.Y;
				case 'z':
					return VirtualKey.Z;


				case 'A':
					return VirtualKey.A;
				case 'B':
					return VirtualKey.B;
				case 'C':
					return VirtualKey.C;
				case 'D':
					return VirtualKey.D;
				case 'E':
					return VirtualKey.E;
				case 'F':
					return VirtualKey.F;
				case 'G':
					return VirtualKey.G;
				case 'H':
					return VirtualKey.H;
				case 'I':
					return VirtualKey.I;
				case 'J':
					return VirtualKey.J;
				case 'K':
					return VirtualKey.K;
				case 'L':
					return VirtualKey.L;
				case 'M':
					return VirtualKey.M;
				case 'N':
					return VirtualKey.N;
				case 'O':
					return VirtualKey.O;
				case 'P':
					return VirtualKey.P;
				case 'Q':
					return VirtualKey.Q;
				case 'R':
					return VirtualKey.R;
				case 'S':
					return VirtualKey.S;
				case 'T':
					return VirtualKey.T;
				case 'U':
					return VirtualKey.U;
				case 'V':
					return VirtualKey.V;
				case 'W':
					return VirtualKey.W;
				case 'X':
					return VirtualKey.X;
				case 'Y':
					return VirtualKey.Y;
				case 'Z':
					return VirtualKey.Z;
				//case .LeftWindows:
				//    return VirtualKey.LeftWindows;
				//case .RightWindows:
				//    return VirtualKey.RightWindows;
				//case .AppSwitch:
				//    return VirtualKey.Application;
				//case .Sleep:
				//    return VirtualKey.Sleep;
				//case .Numpad0:
				//    return VirtualKey.NumberPad0;
				//case .Numpad1:
				//    return VirtualKey.NumberPad1;
				//case .Numpad2:
				//    return VirtualKey.NumberPad2;
				//case .Numpad3:
				//    return VirtualKey.NumberPad3;
				//case .Numpad4:
				//    return VirtualKey.NumberPad4;
				//case .Numpad5:
				//    return VirtualKey.NumberPad5;
				//case .Numpad6:
				//    return VirtualKey.NumberPad6;
				//case .Numpad7:
				//    return VirtualKey.NumberPad7;
				//case .Numpad8:
				//    return VirtualKey.NumberPad8;
				//case .Numpad9:
				//    return VirtualKey.NumberPad9;
				//case .NumpadMultiply:
				//    return VirtualKey.Multiply;
				//case .NumpadAdd:
				//    return VirtualKey.Add;
				//case .NumpadComma:
				//    return VirtualKey.Separator;
				//case .NumpadSubtract:
				//    return VirtualKey.Subtract;
				//case .NumpadDot:
				//    return VirtualKey.Decimal;
				//case .NumpadDivide:
				//    return VirtualKey.Divide;
				//case .F1:
				//    return VirtualKey.F1;
				//case .F2:
				//    return VirtualKey.F2;
				//case .F3:
				//    return VirtualKey.F3;
				//case .F4:
				//    return VirtualKey.F4;
				//case .F5:
				//    return VirtualKey.F5;
				//case .F6:
				//    return VirtualKey.F6;
				//case .F7:
				//    return VirtualKey.F7;
				//case .F8:
				//    return VirtualKey.F8;
				//case .F9:
				//    return VirtualKey.F9;
				//case .F10:
				//    return VirtualKey.F10;
				//case .F11:
				//    return VirtualKey.F11;
				//case .F12:
				//    return VirtualKey.F12;
				//case .F13:
				//    return VirtualKey.F13;
				//case .F14:
				//    return VirtualKey.F14;
				//case .F15:
				//    return VirtualKey.F15;
				//case .F16:
				//    return VirtualKey.F16;
				//case .F17:
				//    return VirtualKey.F17;
				//case .F18:
				//    return VirtualKey.F18;
				//case .F19:
				//    return VirtualKey.F19;
				//case .F20:
				//    return VirtualKey.F20;
				//case .F21:
				//    return VirtualKey.F21;
				//case .F22:
				//    return VirtualKey.F22;
				//case .F23:
				//    return VirtualKey.F23;
				//case .F24:
				//    return VirtualKey.F24;
				//case .NumLock:
				//    return VirtualKey.NumberKeyLock;
				//case .ScrollLock:
				//    return VirtualKey.Scroll;
				//case .ShiftLeft:
				//    return VirtualKey.LeftShift;
				//case .ShiftRight:
				//    return VirtualKey.RightShift;
				//case .CtrlLeft:
				//    return VirtualKey.LeftControl;
				//case .CtrlRight:
				//    return VirtualKey.RightControl;
				//case .LeftMenu:
				//    return VirtualKey.LeftMenu;
				//case .RightMenu:
				//    return VirtualKey.RightMenu;
				//case .GoBack:
				//    return VirtualKey.GoBack;
				//case .GoForward:
				//    return VirtualKey.GoForward;
				//case .Refresh:
				//    return VirtualKey.Refresh;
				//case .MediaStop:
				//    return VirtualKey.Stop;
				//case .Search:
				//    return VirtualKey.Search;
				//case .Favorites:
				//    return VirtualKey.Favorites;
				//case .GoHome:
				//    return VirtualKey.GoHome;
				default:
					{
						if (thisChar.Log().IsEnabled(LogLevel.Warning))
						{
							thisChar.Log().Warn($"Key pressed but not mapped : " + thisChar);
						}
						return VirtualKey.None;
					}
			}
		}
	}
}
