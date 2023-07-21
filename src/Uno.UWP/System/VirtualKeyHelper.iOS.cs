// Some mappings based on
// https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
// https://lists.w3.org/Archives/Public/www-dom/2010JulSep/att-0182/keyCode-spec.html

using System;
using Foundation;
using UIKit;

namespace Windows.System
{
	internal static class VirtualKeyHelper
	{
		public static VirtualKey FromKeyCode(UIKeyboardHidUsage keyCode)
			=> keyCode switch
			{
				UIKeyboardHidUsage.Keyboard0 => VirtualKey.Number0,
				UIKeyboardHidUsage.Keyboard1 => VirtualKey.Number1,
				UIKeyboardHidUsage.Keyboard2 => VirtualKey.Number2,
				UIKeyboardHidUsage.Keyboard3 => VirtualKey.Number3,
				UIKeyboardHidUsage.Keyboard4 => VirtualKey.Number4,
				UIKeyboardHidUsage.Keyboard5 => VirtualKey.Number5,
				UIKeyboardHidUsage.Keyboard6 => VirtualKey.Number6,
				UIKeyboardHidUsage.Keyboard7 => VirtualKey.Number7,
				UIKeyboardHidUsage.Keyboard8 => VirtualKey.Number8,
				UIKeyboardHidUsage.Keyboard9 => VirtualKey.Number9,

				UIKeyboardHidUsage.KeyboardA => VirtualKey.A,
				UIKeyboardHidUsage.KeyboardB => VirtualKey.B,
				UIKeyboardHidUsage.KeyboardC => VirtualKey.C,
				UIKeyboardHidUsage.KeyboardD => VirtualKey.D,
				UIKeyboardHidUsage.KeyboardE => VirtualKey.E,
				UIKeyboardHidUsage.KeyboardF => VirtualKey.F,
				UIKeyboardHidUsage.KeyboardG => VirtualKey.G,
				UIKeyboardHidUsage.KeyboardH => VirtualKey.H,
				UIKeyboardHidUsage.KeyboardI => VirtualKey.I,
				UIKeyboardHidUsage.KeyboardJ => VirtualKey.J,
				UIKeyboardHidUsage.KeyboardK => VirtualKey.K,
				UIKeyboardHidUsage.KeyboardL => VirtualKey.L,
				UIKeyboardHidUsage.KeyboardM => VirtualKey.M,
				UIKeyboardHidUsage.KeyboardN => VirtualKey.N,
				UIKeyboardHidUsage.KeyboardO => VirtualKey.O,
				UIKeyboardHidUsage.KeyboardP => VirtualKey.P,
				UIKeyboardHidUsage.KeyboardQ => VirtualKey.Q,
				UIKeyboardHidUsage.KeyboardR => VirtualKey.R,
				UIKeyboardHidUsage.KeyboardS => VirtualKey.S,
				UIKeyboardHidUsage.KeyboardT => VirtualKey.T,
				UIKeyboardHidUsage.KeyboardU => VirtualKey.U,
				UIKeyboardHidUsage.KeyboardV => VirtualKey.V,
				UIKeyboardHidUsage.KeyboardW => VirtualKey.W,
				UIKeyboardHidUsage.KeyboardX => VirtualKey.X,
				UIKeyboardHidUsage.KeyboardY => VirtualKey.Y,
				UIKeyboardHidUsage.KeyboardZ => VirtualKey.Z,

				UIKeyboardHidUsage.KeyboardPeriod => VirtualKey.Decimal,
				UIKeyboardHidUsage.KeyboardEqualSign => (VirtualKey)187,
				UIKeyboardHidUsage.KeyboardSlash => VirtualKey.Divide,
				UIKeyboardHidUsage.KeyboardHyphen => VirtualKey.Subtract,

				// [Key|Number] Pad
				UIKeyboardHidUsage.Keypad0 => VirtualKey.NumberPad0,
				UIKeyboardHidUsage.Keypad1 => VirtualKey.NumberPad1,
				UIKeyboardHidUsage.Keypad2 => VirtualKey.NumberPad2,
				UIKeyboardHidUsage.Keypad3 => VirtualKey.NumberPad3,
				UIKeyboardHidUsage.Keypad4 => VirtualKey.NumberPad4,
				UIKeyboardHidUsage.Keypad5 => VirtualKey.NumberPad5,
				UIKeyboardHidUsage.Keypad6 => VirtualKey.NumberPad6,
				UIKeyboardHidUsage.Keypad7 => VirtualKey.NumberPad7,
				UIKeyboardHidUsage.Keypad8 => VirtualKey.NumberPad8,
				UIKeyboardHidUsage.Keypad9 => VirtualKey.NumberPad9,

				UIKeyboardHidUsage.KeypadPeriod => VirtualKey.Decimal,
				UIKeyboardHidUsage.KeypadAsterisk => VirtualKey.Multiply,
				UIKeyboardHidUsage.KeypadPlus => VirtualKey.Add,
				UIKeyboardHidUsage.KeypadSlash => VirtualKey.Divide,
				UIKeyboardHidUsage.KeypadHyphen => VirtualKey.Subtract,
				UIKeyboardHidUsage.KeypadEnter => VirtualKey.Enter, // =
				UIKeyboardHidUsage.KeyboardClear => VirtualKey.Clear,

				UIKeyboardHidUsage.KeyboardSpacebar => VirtualKey.Space,
				UIKeyboardHidUsage.KeyboardReturnOrEnter => VirtualKey.Enter,
				UIKeyboardHidUsage.KeyboardTab => VirtualKey.Tab,
				UIKeyboardHidUsage.KeyboardDeleteForward => VirtualKey.Delete,
				UIKeyboardHidUsage.KeyboardDeleteOrBackspace => VirtualKey.Delete,

				// Modifiers
				UIKeyboardHidUsage.KeyboardEscape => VirtualKey.Escape,
				UIKeyboardHidUsage.KeyboardLeftGui => VirtualKey.LeftWindows, // Command
				UIKeyboardHidUsage.KeyboardRightGui => VirtualKey.RightWindows, // Command
				UIKeyboardHidUsage.KeyboardLeftShift => VirtualKey.Shift, // Left Shift
				UIKeyboardHidUsage.KeyboardCapsLock => VirtualKey.CapitalLock,
				UIKeyboardHidUsage.KeyboardLeftAlt => VirtualKey.LeftMenu,
				UIKeyboardHidUsage.KeyboardLeftControl => VirtualKey.Control, // Left control
				UIKeyboardHidUsage.KeyboardRightShift => VirtualKey.RightShift,
				UIKeyboardHidUsage.KeyboardRightAlt => VirtualKey.RightMenu, // Right option, a.k.a. Right Alt
				UIKeyboardHidUsage.KeyboardRightControl => VirtualKey.RightControl,

				// Functions
				UIKeyboardHidUsage.KeyboardF1 => VirtualKey.F1,
				UIKeyboardHidUsage.KeyboardF2 => VirtualKey.F2,
				UIKeyboardHidUsage.KeyboardF3 => VirtualKey.F3,
				UIKeyboardHidUsage.KeyboardF4 => VirtualKey.F4,
				UIKeyboardHidUsage.KeyboardF5 => VirtualKey.F5,
				UIKeyboardHidUsage.KeyboardF6 => VirtualKey.F6,
				UIKeyboardHidUsage.KeyboardF7 => VirtualKey.F7,
				UIKeyboardHidUsage.KeyboardF8 => VirtualKey.F8,
				UIKeyboardHidUsage.KeyboardF9 => VirtualKey.F9,
				UIKeyboardHidUsage.KeyboardF10 => VirtualKey.F10,
				UIKeyboardHidUsage.KeyboardF11 => VirtualKey.F11,
				UIKeyboardHidUsage.KeyboardF12 => VirtualKey.F12,
				UIKeyboardHidUsage.KeyboardF13 => VirtualKey.F13,
				UIKeyboardHidUsage.KeyboardF14 => VirtualKey.F14,
				UIKeyboardHidUsage.KeyboardF15 => VirtualKey.F15,
				UIKeyboardHidUsage.KeyboardF16 => VirtualKey.F16,
				UIKeyboardHidUsage.KeyboardF17 => VirtualKey.F17,
				UIKeyboardHidUsage.KeyboardF18 => VirtualKey.F18,
				UIKeyboardHidUsage.KeyboardF19 => VirtualKey.F19,
				UIKeyboardHidUsage.KeyboardF20 => VirtualKey.F20,
				UIKeyboardHidUsage.KeyboardF21 => VirtualKey.F21,
				UIKeyboardHidUsage.KeyboardF22 => VirtualKey.F22,
				UIKeyboardHidUsage.KeyboardF23 => VirtualKey.F23,
				UIKeyboardHidUsage.KeyboardF24 => VirtualKey.F24,

				// Navigation
				UIKeyboardHidUsage.KeyboardInsert => VirtualKey.Insert,
				UIKeyboardHidUsage.KeyboardHome => VirtualKey.Home,
				UIKeyboardHidUsage.KeyboardEnd => VirtualKey.End,
				UIKeyboardHidUsage.KeyboardPageUp => VirtualKey.PageUp,
				UIKeyboardHidUsage.KeyboardPageDown => VirtualKey.PageDown,
				UIKeyboardHidUsage.KeyboardLeftArrow => VirtualKey.Left,
				UIKeyboardHidUsage.KeyboardRightArrow => VirtualKey.Right,
				UIKeyboardHidUsage.KeyboardDownArrow => VirtualKey.Down,
				UIKeyboardHidUsage.KeyboardUpArrow => VirtualKey.Up,

				_ => VirtualKey.None,
			};

	}
}

