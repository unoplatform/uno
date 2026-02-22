using System;

namespace Windows.System
{
	internal static partial class BrowserVirtualKeyHelper
	{
		public static VirtualKey FromKey(string key)
		{
			// https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/key
			// https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/key/Key_Values
			switch (key)
			{
				// Special
				case "Unidentified":
					return VirtualKey.None;
				// Modifier
				case "CapsLock":
					return VirtualKey.CapitalLock;
				case "Control":
					return VirtualKey.Control;
				case "Meta":
					return VirtualKey.LeftWindows;
				case "NumLock":
					return VirtualKey.NumberKeyLock;
				case "ScrollLock":
					return VirtualKey.Scroll;
				case "Shift":
					return VirtualKey.Shift;
				case "SymbolLock":
				// Whitespace
				case "Enter":
					return VirtualKey.Enter;
				case "Tab":
					return VirtualKey.Tab;
				case " ":
					return VirtualKey.Space;
				// Navigation
				case "ArrowDown":
					return VirtualKey.Down;
				case "ArrowLeft":
					return VirtualKey.Left;
				case "ArrowRight":
					return VirtualKey.Right;
				case "ArrowUp":
					return VirtualKey.Up;
				case "End":
					return VirtualKey.End;
				case "Home":
					return VirtualKey.Home;
				case "PageDown":
					return VirtualKey.PageDown;
				case "PageUp":
					return VirtualKey.PageUp;
				// Editing
				case "Backspace":
					return VirtualKey.Back;
				case "Delete":
					return VirtualKey.Delete;
				case "Insert":
					return VirtualKey.Insert;
				// UI
				case "Accept":
					return VirtualKey.Accept;
				case "Cancel":
					return VirtualKey.Cancel;
				case "ContextMenu":
					return VirtualKey.Menu;
				case "Escape":
					return VirtualKey.Escape;
				case "Execute":
					return VirtualKey.Execute;
				case "Help":
					return VirtualKey.Help;
				case "Pause":
					return VirtualKey.Pause;
				case "Select":
					return VirtualKey.Select;
				// IME
				case "NonConvert":
					return VirtualKey.NonConvert;
				case "Convert":
					return VirtualKey.Convert;
				case "FinalMode":
					return VirtualKey.Final;
				case "ModeChange":
					return VirtualKey.ModeChange;
				// Korean
				case "HangulMode":
					return VirtualKey.Hangul;
				case "HanjalMode":
					return VirtualKey.Hanja;
				case "JunjaMode":
					return VirtualKey.Junja;
				// Japanese
				case "KanaMode":
					return VirtualKey.Kana;
				case "KanjiMode":
					return VirtualKey.Kanji;
				// Function
				case "F1":
					return VirtualKey.F1;
				case "F2":
					return VirtualKey.F2;
				case "F3":
					return VirtualKey.F3;
				case "F4":
					return VirtualKey.F4;
				case "F5":
					return VirtualKey.F5;
				case "F6":
					return VirtualKey.F6;
				case "F7":
					return VirtualKey.F7;
				case "F8":
					return VirtualKey.F8;
				case "F9":
					return VirtualKey.F9;
				case "F10":
					return VirtualKey.F10;
				case "F11":
					return VirtualKey.F11;
				case "F12":
					return VirtualKey.F12;
				case "F13":
					return VirtualKey.F13;
				case "F14":
					return VirtualKey.F14;
				case "F15":
					return VirtualKey.F15;
				case "F16":
					return VirtualKey.F16;
				case "F17":
					return VirtualKey.F17;
				case "F18":
					return VirtualKey.F18;
				case "F19":
					return VirtualKey.F19;
				case "F20":
					return VirtualKey.F20;
				// Document
				case "Print":
					return VirtualKey.Print;
				// Browser control
				case "BrowserBack":
					return VirtualKey.GoBack;
				case "BrowserFavorites":
					return VirtualKey.Favorites;
				case "BrowserForward":
					return VirtualKey.GoForward;
				case "BrowserHome":
					return VirtualKey.GoHome;
				case "BrowserRefresh":
					return VirtualKey.Refresh;
				case "BrowserSearch":
					return VirtualKey.Search;
				case "BrowserStop":
					return VirtualKey.Stop;
				// Numeric keypad
				case "Decimal":
				case ".":
					return VirtualKey.Decimal;
				case "Multiply":
				case "*":
					return VirtualKey.Multiply;
				case "Add":
				case "+":
					return VirtualKey.Add;
				case "Clear":
					return VirtualKey.Clear;
				case "Divide":
				case "/":
					return VirtualKey.Divide;
				case "Subtract":
				case "-":
					return VirtualKey.Subtract;
				case "Separator":
					return VirtualKey.Separator;
				case "0":
					return VirtualKey.Number0;
				case "1":
					return VirtualKey.Number1;
				case "2":
					return VirtualKey.Number2;
				case "3":
					return VirtualKey.Number3;
				case "4":
					return VirtualKey.Number4;
				case "5":
					return VirtualKey.Number5;
				case "6":
					return VirtualKey.Number6;
				case "7":
					return VirtualKey.Number7;
				case "8":
					return VirtualKey.Number8;
				case "9":
					return VirtualKey.Number9;
				// Letter
				case "a":
				case "A":
					return VirtualKey.A;
				case "b":
				case "B":
					return VirtualKey.B;
				case "c":
				case "C":
					return VirtualKey.C;
				case "d":
				case "D":
					return VirtualKey.D;
				case "e":
				case "E":
					return VirtualKey.E;
				case "f":
				case "F":
					return VirtualKey.F;
				case "g":
				case "G":
					return VirtualKey.G;
				case "h":
				case "H":
					return VirtualKey.H;
				case "i":
				case "I":
					return VirtualKey.I;
				case "j":
				case "J":
					return VirtualKey.J;
				case "k":
				case "K":
					return VirtualKey.K;
				case "l":
				case "L":
					return VirtualKey.L;
				case "m":
				case "M":
					return VirtualKey.M;
				case "n":
				case "N":
					return VirtualKey.N;
				case "o":
				case "O":
					return VirtualKey.O;
				case "p":
				case "P":
					return VirtualKey.P;
				case "q":
				case "Q":
					return VirtualKey.Q;
				case "r":
				case "R":
					return VirtualKey.R;
				case "s":
				case "S":
					return VirtualKey.S;
				case "t":
				case "T":
					return VirtualKey.T;
				case "u":
				case "U":
					return VirtualKey.U;
				case "v":
				case "V":
					return VirtualKey.V;
				case "w":
				case "W":
					return VirtualKey.W;
				case "x":
				case "X":
					return VirtualKey.X;
				case "y":
				case "Y":
					return VirtualKey.Y;
				case "z":
				case "Z":
					return VirtualKey.Z;
				default:
					return VirtualKey.None;
			}
		}

		// https://developer.mozilla.org/en-US/docs/Web/API/UI_Events/Keyboard_event_code_values
		public static VirtualKey FromCode(string code)
		{
			switch (code)
			{
				case "Space":
					return VirtualKey.Space;
				case "ShiftRight":
					return VirtualKey.Shift; // NOT LeftShift
				case "ShiftLeft":
					return VirtualKey.Shift;
				case "ControlLeft":
					return VirtualKey.Control;
				case "ControlRight":
					return VirtualKey.Control;
				case "MetaRight":
					return VirtualKey.LeftWindows;
				case "MetaLeft":
					return VirtualKey.RightWindows;
				case "AltLeft":
					return VirtualKey.Menu;
				case "AltRight":
					return VirtualKey.Menu;
				case "NumpadMultiply":
					return VirtualKey.Multiply;
				case "NumpadAdd":
					return VirtualKey.Add;
				case "NumpadSubtract":
					return VirtualKey.Subtract;
				case "NumpadDivide":
					return VirtualKey.Divide;
				case "NumpadDecimal":
					return VirtualKey.Decimal;
				case "Numpad0":
					return VirtualKey.Number0;
				case "Numpad1":
					return VirtualKey.Number1;
				case "Numpad2":
					return VirtualKey.Number2;
				case "Numpad3":
					return VirtualKey.Number3;
				case "Numpad4":
					return VirtualKey.Number4;
				case "Numpad5":
					return VirtualKey.Number5;
				case "Numpad6":
					return VirtualKey.Number6;
				case "Numpad7":
					return VirtualKey.Number7;
				case "Numpad8":
					return VirtualKey.Number8;
				case "Numpad9":
					return VirtualKey.Number9;
			}

			// A lot of keys and codes are similar.
			var fromKey = FromKey(code);
			if (fromKey is not VirtualKey.None)
			{
				return fromKey;
			}

			if (code.StartsWith("Key", StringComparison.Ordinal))
			{
				// map KeyA to VirtualKey.A, KeyB to VirtualKey.B, etc.
				return VirtualKey.A + code[3] - 'A';
			}

			if (code.StartsWith("Digit", StringComparison.Ordinal))
			{
				// map Digit0 to VirtualKey.Number0, etc.
				return VirtualKey.Number0 + code[5] - '0';
			}

			// WinUI sometimes gives unnamed int values to keys like Comma (188) and Period (190).
			// Since these don't result in a valid enum value, we ignore these.
			return VirtualKey.None;
		}
	}
}
