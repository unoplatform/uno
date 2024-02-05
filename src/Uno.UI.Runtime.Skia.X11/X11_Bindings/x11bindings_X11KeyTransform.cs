// The MIT License (MIT)
//
// Copyright (c) .NET Foundation and Contributors
// All Rights Reserved
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// https://github.com/AvaloniaUI/Avalonia/blob/5fa3ffaeab7e5cd2662ef02d03e34b9d4cb1a489/src/Avalonia.X11/X11KeyTransform.cs

using System;
using System.Collections.Generic;
using Windows.System;

namespace Uno.WinUI.Runtime.Skia.X11
{
	public static class X11KeyTransform
	{
		// TODO: uncomment and fix as needed
		private static readonly Dictionary<X11Key, VirtualKey> s_keyFromX11Key = new()
		{
			{ X11Key.Cancel, VirtualKey.Cancel },
			{ X11Key.BackSpace, VirtualKey.Back },
			{ X11Key.Tab, VirtualKey.Tab },
			// {X11Key.Linefeed, VirtualKey.LineFeed},
			{ X11Key.Clear, VirtualKey.Clear },
			{ X11Key.Return, VirtualKey.Enter },
			{ X11Key.KP_Enter, VirtualKey.Enter },
			{ X11Key.Pause, VirtualKey.Pause },
			{ X11Key.Caps_Lock, VirtualKey.CapitalLock },
			//{ X11Key.?, VirtualKey.HangulMode }
			//{ X11Key.?, VirtualKey.JunjaMode }
			//{ X11Key.?, VirtualKey.FinalMode }
			//{ X11Key.?, VirtualKey.KanjiMode }
			{ X11Key.Escape, VirtualKey.Escape },
			//{ X11Key.?, VirtualKey.ImeConvert }
			//{ X11Key.?, VirtualKey.ImeNonConvert }
			//{ X11Key.?, VirtualKey.ImeAccept }
			//{ X11Key.?, VirtualKey.ImeModeChange }
			{ X11Key.space, VirtualKey.Space },
			// {X11Key.Prior, VirtualKey.Prior},
			// {X11Key.KP_Prior, VirtualKey.Prior},
			{ X11Key.Page_Down, VirtualKey.PageDown },
			{ X11Key.KP_Page_Down, VirtualKey.PageDown },
			{ X11Key.End, VirtualKey.End },
			{ X11Key.KP_End, VirtualKey.End },
			{ X11Key.Home, VirtualKey.Home },
			{ X11Key.KP_Home, VirtualKey.Home },
			{ X11Key.Left, VirtualKey.Left },
			{ X11Key.KP_Left, VirtualKey.Left },
			{ X11Key.Up, VirtualKey.Up },
			{ X11Key.KP_Up, VirtualKey.Up },
			{ X11Key.Right, VirtualKey.Right },
			{ X11Key.KP_Right, VirtualKey.Right },
			{ X11Key.Down, VirtualKey.Down },
			{ X11Key.KP_Down, VirtualKey.Down },
			{ X11Key.Select, VirtualKey.Select },
			{ X11Key.Print, VirtualKey.Print },
			{ X11Key.Execute, VirtualKey.Execute },
			//{ X11Key.?, VirtualKey.Snapshot }
			{ X11Key.Insert, VirtualKey.Insert },
			{ X11Key.KP_Insert, VirtualKey.Insert },
			{ X11Key.Delete, VirtualKey.Delete },
			{ X11Key.KP_Delete, VirtualKey.Delete },
			{ X11Key.Help, VirtualKey.Help },
			{ X11Key.A, VirtualKey.A },
			{ X11Key.B, VirtualKey.B },
			{ X11Key.C, VirtualKey.C },
			{ X11Key.D, VirtualKey.D },
			{ X11Key.E, VirtualKey.E },
			{ X11Key.F, VirtualKey.F },
			{ X11Key.G, VirtualKey.G },
			{ X11Key.H, VirtualKey.H },
			{ X11Key.I, VirtualKey.I },
			{ X11Key.J, VirtualKey.J },
			{ X11Key.K, VirtualKey.K },
			{ X11Key.L, VirtualKey.L },
			{ X11Key.M, VirtualKey.M },
			{ X11Key.N, VirtualKey.N },
			{ X11Key.O, VirtualKey.O },
			{ X11Key.P, VirtualKey.P },
			{ X11Key.Q, VirtualKey.Q },
			{ X11Key.R, VirtualKey.R },
			{ X11Key.S, VirtualKey.S },
			{ X11Key.T, VirtualKey.T },
			{ X11Key.U, VirtualKey.U },
			{ X11Key.V, VirtualKey.V },
			{ X11Key.W, VirtualKey.W },
			{ X11Key.X, VirtualKey.X },
			{ X11Key.Y, VirtualKey.Y },
			{ X11Key.Z, VirtualKey.Z },
			{ X11Key.a, VirtualKey.A },
			{ X11Key.b, VirtualKey.B },
			{ X11Key.c, VirtualKey.C },
			{ X11Key.d, VirtualKey.D },
			{ X11Key.e, VirtualKey.E },
			{ X11Key.f, VirtualKey.F },
			{ X11Key.g, VirtualKey.G },
			{ X11Key.h, VirtualKey.H },
			{ X11Key.i, VirtualKey.I },
			{ X11Key.j, VirtualKey.J },
			{ X11Key.k, VirtualKey.K },
			{ X11Key.l, VirtualKey.L },
			{ X11Key.m, VirtualKey.M },
			{ X11Key.n, VirtualKey.N },
			{ X11Key.o, VirtualKey.O },
			{ X11Key.p, VirtualKey.P },
			{ X11Key.q, VirtualKey.Q },
			{ X11Key.r, VirtualKey.R },
			{ X11Key.s, VirtualKey.S },
			{ X11Key.t, VirtualKey.T },
			{ X11Key.u, VirtualKey.U },
			{ X11Key.v, VirtualKey.V },
			{ X11Key.w, VirtualKey.W },
			{ X11Key.x, VirtualKey.X },
			{ X11Key.y, VirtualKey.Y },
			{ X11Key.z, VirtualKey.Z },
			{ X11Key.Super_L, VirtualKey.LeftWindows },
			{ X11Key.Super_R, VirtualKey.RightWindows },
			{ X11Key.Menu, VirtualKey.Menu },
			//{ X11Key.?, VirtualKey.Sleep }
			{ X11Key.KP_0, VirtualKey.NumberPad0 },
			{ X11Key.KP_1, VirtualKey.NumberPad1 },
			{ X11Key.KP_2, VirtualKey.NumberPad2 },
			{ X11Key.KP_3, VirtualKey.NumberPad3 },
			{ X11Key.KP_4, VirtualKey.NumberPad4 },
			{ X11Key.KP_5, VirtualKey.NumberPad5 },
			{ X11Key.KP_6, VirtualKey.NumberPad6 },
			{ X11Key.KP_7, VirtualKey.NumberPad7 },
			{ X11Key.KP_8, VirtualKey.NumberPad8 },
			{ X11Key.KP_9, VirtualKey.NumberPad9 },
			{ X11Key.multiply, VirtualKey.Multiply },
			{ X11Key.KP_Multiply, VirtualKey.Multiply },
			{ X11Key.KP_Add, VirtualKey.Add },
			//{ X11Key.?, VirtualKey.Separator }
			{ X11Key.KP_Subtract, VirtualKey.Subtract },
			{ X11Key.KP_Decimal, VirtualKey.Decimal },
			{ X11Key.KP_Divide, VirtualKey.Divide },
			{ X11Key.F1, VirtualKey.F1 },
			{ X11Key.F2, VirtualKey.F2 },
			{ X11Key.F3, VirtualKey.F3 },
			{ X11Key.F4, VirtualKey.F4 },
			{ X11Key.F5, VirtualKey.F5 },
			{ X11Key.F6, VirtualKey.F6 },
			{ X11Key.F7, VirtualKey.F7 },
			{ X11Key.F8, VirtualKey.F8 },
			{ X11Key.F9, VirtualKey.F9 },
			{ X11Key.F10, VirtualKey.F10 },
			{ X11Key.F11, VirtualKey.F11 },
			{ X11Key.F12, VirtualKey.F12 },
			{ X11Key.L3, VirtualKey.F13 },
			{ X11Key.F14, VirtualKey.F14 },
			{ X11Key.L5, VirtualKey.F15 },
			{ X11Key.F16, VirtualKey.F16 },
			{ X11Key.F17, VirtualKey.F17 },
			{ X11Key.L8, VirtualKey.F18 },
			{ X11Key.L9, VirtualKey.F19 },
			{ X11Key.L10, VirtualKey.F20 },
			{ X11Key.R1, VirtualKey.F21 },
			{ X11Key.R2, VirtualKey.F22 },
			{ X11Key.F23, VirtualKey.F23 },
			{ X11Key.R4, VirtualKey.F24 },
			{ X11Key.Num_Lock, VirtualKey.NumberKeyLock },
			{ X11Key.Scroll_Lock, VirtualKey.Scroll },
			{ X11Key.Shift_L, VirtualKey.LeftShift },
			{ X11Key.Shift_R, VirtualKey.RightShift },
			{ X11Key.Control_L, VirtualKey.LeftControl },
			{ X11Key.Control_R, VirtualKey.RightControl },
			{ X11Key.Alt_L, VirtualKey.LeftMenu },
			{ X11Key.Alt_R, VirtualKey.RightMenu },
			//{ X11Key.?, VirtualKey.BrowserBack }
			//{ X11Key.?, VirtualKey.BrowserForward }
			//{ X11Key.?, VirtualKey.BrowserRefresh }
			//{ X11Key.?, VirtualKey.BrowserStop }
			//{ X11Key.?, VirtualKey.BrowserSearch }
			//{ X11Key.?, VirtualKey.BrowserFavorites }
			//{ X11Key.?, VirtualKey.BrowserHome }
			//{ X11Key.?, VirtualKey.VolumeMute }
			//{ X11Key.?, VirtualKey.VolumeDown }
			//{ X11Key.?, VirtualKey.VolumeUp }
			//{ X11Key.?, VirtualKey.MediaNextTrack }
			//{ X11Key.?, VirtualKey.MediaPreviousTrack }
			//{ X11Key.?, VirtualKey.MediaStop }
			//{ X11Key.?, VirtualKey.MediaPlayPause }
			//{ X11Key.?, VirtualKey.LaunchMail }
			//{ X11Key.?, VirtualKey.SelectMedia }
			//{ X11Key.?, VirtualKey.LaunchApplication1 }
			//{ X11Key.?, VirtualKey.LaunchApplication2 }
			// {X11Key.minus, VirtualKey.OemMinus},
			// {X11Key.underscore, VirtualKey.OemMinus},
			// {X11Key.plus, VirtualKey.OemPlus},
			// {X11Key.equal, VirtualKey.OemPlus},
			// {X11Key.bracketleft, VirtualKey.OemOpenBrackets},
			// {X11Key.braceleft, VirtualKey.OemOpenBrackets},
			// {X11Key.bracketright, VirtualKey.OemCloseBrackets},
			// {X11Key.braceright, VirtualKey.OemCloseBrackets},
			// {X11Key.backslash, VirtualKey.OemPipe},
			// {X11Key.bar, VirtualKey.OemPipe},
			// {X11Key.semicolon, VirtualKey.OemSemicolon},
			// {X11Key.colon, VirtualKey.OemSemicolon},
			// {X11Key.apostrophe, VirtualKey.OemQuotes},
			// {X11Key.quotedbl, VirtualKey.OemQuotes},
			// {X11Key.comma, VirtualKey.OemComma},
			// {X11Key.less, VirtualKey.OemComma},
			// {X11Key.period, VirtualKey.OemPeriod},
			// {X11Key.greater, VirtualKey.OemPeriod},
			// {X11Key.slash, VirtualKey.Oem2},
			// {X11Key.question, VirtualKey.Oem2},
			// {X11Key.grave, VirtualKey.OemTilde},
			// {X11Key.asciitilde, VirtualKey.OemTilde},
			{ X11Key.XK_1, VirtualKey.Number1 },
			{ X11Key.XK_2, VirtualKey.Number2 },
			{ X11Key.XK_3, VirtualKey.Number3 },
			{ X11Key.XK_4, VirtualKey.Number4 },
			{ X11Key.XK_5, VirtualKey.Number5 },
			{ X11Key.XK_6, VirtualKey.Number6 },
			{ X11Key.XK_7, VirtualKey.Number7 },
			{ X11Key.XK_8, VirtualKey.Number8 },
			{ X11Key.XK_9, VirtualKey.Number9 },
			{ X11Key.XK_0, VirtualKey.Number0 },
			//{ X11Key.?, VirtualKey.AbntC1 }
			//{ X11Key.?, VirtualKey.AbntC2 }
			//{ X11Key.?, VirtualKey.Oem8 }
			//{ X11Key.?, VirtualKey.Oem102 }
			//{ X11Key.?, VirtualKey.ImeProcessed }
			//{ X11Key.?, VirtualKey.System }
			//{ X11Key.?, VirtualKey.OemAttn }
			//{ X11Key.?, VirtualKey.OemFinish }
			//{ X11Key.?, VirtualKey.DbeHiragana }
			//{ X11Key.?, VirtualKey.OemAuto }
			//{ X11Key.?, VirtualKey.DbeDbcsChar }
			//{ X11Key.?, VirtualKey.OemBackTab }
			//{ X11Key.?, VirtualKey.Attn }
			//{ X11Key.?, VirtualKey.DbeEnterWordRegisterMode }
			//{ X11Key.?, VirtualKey.DbeEnterImeConfigureMode }
			//{ X11Key.?, VirtualKey.EraseEof }
			//{ X11Key.?, VirtualKey.Play }
			//{ X11Key.?, VirtualKey.Zoom }
			//{ X11Key.?, VirtualKey.NoName }
			//{ X11Key.?, VirtualKey.DbeEnterDialogConversionMode }
			//{ X11Key.?, VirtualKey.OemClear }
			//{ X11Key.?, VirtualKey.DeadCharProcessed }
		};

		public static VirtualKey VirtualKeyFromKeySym(IntPtr key)
			=> s_keyFromX11Key.TryGetValue((X11Key)key, out var result) ? result : VirtualKey.None;
	}

}
