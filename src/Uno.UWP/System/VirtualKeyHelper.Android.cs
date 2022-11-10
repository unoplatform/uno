using Android.Views;

namespace Windows.System;

internal static class VirtualKeyHelper
{
	//TODO: Add remaining keycodes
	public static VirtualKey FromKeyCode(Keycode key)
		=> key switch
		{
			Keycode.Num0 => VirtualKey.Number0,
			Keycode.Num1 => VirtualKey.Number1,
			Keycode.Num2 => VirtualKey.Number2,
			Keycode.Num3 => VirtualKey.Number3,
			Keycode.Num4 => VirtualKey.Number4,
			Keycode.Num5 => VirtualKey.Number5,
			Keycode.Num6 => VirtualKey.Number6,
			Keycode.Num7 => VirtualKey.Number7,
			Keycode.Num8 => VirtualKey.Number8,
			Keycode.Num9 => VirtualKey.Number9,

			Keycode.Numpad0 => VirtualKey.NumberPad0,
			Keycode.Numpad1 => VirtualKey.NumberPad1,
			Keycode.Numpad2 => VirtualKey.NumberPad2,
			Keycode.Numpad3 => VirtualKey.NumberPad3,
			Keycode.Numpad4 => VirtualKey.NumberPad4,
			Keycode.Numpad5 => VirtualKey.NumberPad5,
			Keycode.Numpad6 => VirtualKey.NumberPad6,
			Keycode.Numpad7 => VirtualKey.NumberPad7,
			Keycode.Numpad8 => VirtualKey.NumberPad8,
			Keycode.Numpad9 => VirtualKey.NumberPad9,

			Keycode.A => VirtualKey.A,
			Keycode.B => VirtualKey.B,
			Keycode.C => VirtualKey.C,
			Keycode.D => VirtualKey.D,
			Keycode.E => VirtualKey.E,
			Keycode.F => VirtualKey.F,
			Keycode.G => VirtualKey.G,
			Keycode.H => VirtualKey.H,
			Keycode.I => VirtualKey.I,
			Keycode.J => VirtualKey.J,
			Keycode.K => VirtualKey.K,
			Keycode.L => VirtualKey.L,
			Keycode.M => VirtualKey.M,
			Keycode.N => VirtualKey.N,
			Keycode.O => VirtualKey.O,
			Keycode.P => VirtualKey.P,
			Keycode.Q => VirtualKey.Q,
			Keycode.R => VirtualKey.R,
			Keycode.S => VirtualKey.S,
			Keycode.T => VirtualKey.T,
			Keycode.U => VirtualKey.U,
			Keycode.V => VirtualKey.V,
			Keycode.W => VirtualKey.W,
			Keycode.X => VirtualKey.X,
			Keycode.Y => VirtualKey.Y,
			Keycode.Z => VirtualKey.Z,

			Keycode.Period => VirtualKey.Decimal,
			// TODO: Keycode.Equals && NumpadEquals => VirtualKey.,
			Keycode.NumpadDivide => VirtualKey.Divide,
			Keycode.NumpadSubtract => VirtualKey.Subtract,

			Keycode.NumpadMultiply => VirtualKey.Multiply,
			Keycode.NumpadAdd => VirtualKey.Add,
			Keycode.Enter => VirtualKey.Enter,
			Keycode.NumpadEnter => VirtualKey.Enter,
			Keycode.Clear => VirtualKey.Clear,

			Keycode.Space => VirtualKey.Space,
			Keycode.Tab => VirtualKey.Tab,
			//Keycode.Del => VirtualKey.Eras,
			Keycode.ForwardDel => VirtualKey.Delete,

			// Modifiers
			Keycode.Escape => VirtualKey.Escape,
			Keycode.MetaLeft => VirtualKey.LeftWindows, // Command
			//Keycode.KeyboardRightGui => VirtualKey.RightWindows, // Command
			//Keycode.KeyboardLeftShift => VirtualKey.Shift, // Left Shift
			//Keycode.KeyboardCapsLock => VirtualKey.CapitalLock,
			//Keycode.KeyboardLeftAlt => VirtualKey.LeftMenu,
			//Keycode.KeyboardLeftControl => VirtualKey.Control, // Left control
			//Keycode.KeyboardRightShift => VirtualKey.RightShift,
			//Keycode.KeyboardRightAlt => VirtualKey.RightMenu, // Right option, a.k.a. Right Alt
			//Keycode.KeyboardRightControl => VirtualKey.RightControl,

			// Functions
			Keycode.F1 => VirtualKey.F1,
			Keycode.F2 => VirtualKey.F2,
			Keycode.F3 => VirtualKey.F3,
			Keycode.F4 => VirtualKey.F4,
			Keycode.F5 => VirtualKey.F5,
			Keycode.F6 => VirtualKey.F6,
			Keycode.F7 => VirtualKey.F7,
			Keycode.F8 => VirtualKey.F8,
			Keycode.F9 => VirtualKey.F9,
			Keycode.F10 => VirtualKey.F10,
			Keycode.F11 => VirtualKey.F11,
			Keycode.F12 => VirtualKey.F12,

			// Navigation
			Keycode.Insert => VirtualKey.Insert,
			Keycode.Home => VirtualKey.Home,
			//Keycode.End => VirtualKey.End,
			Keycode.PageUp => VirtualKey.PageUp,
			Keycode.PageDown => VirtualKey.PageDown,
			//Keycode.Left => VirtualKey.Left,
			//Keycode.RightArrow => VirtualKey.Right,
			//Keycode.DownArrow => VirtualKey.Down,
			//Keycode.UpArrow => VirtualKey.Up,
			_ => VirtualKey.None
		};
}
