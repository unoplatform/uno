using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.System
{
	internal static partial class VirtualKeyHelper
	{
		// http://macbiblioblog.blogspot.com/2014/12/key-codes-for-function-and-special-keys.html
		public static VirtualKey FromKeyCode(ushort keyCode)
			=> keyCode switch
			{
				29	=> VirtualKey.Number0,
				18	=> VirtualKey.Number1,
				19	=> VirtualKey.Number2,
				20	=> VirtualKey.Number3,
				21	=> VirtualKey.Number4,
				23	=> VirtualKey.Number5,
				22	=> VirtualKey.Number6,
				26	=> VirtualKey.Number7,
				28	=> VirtualKey.Number8,
				25	=> VirtualKey.Number9,

				0	=> VirtualKey.A,
				11	=> VirtualKey.B,
				8	=> VirtualKey.C,
				2	=> VirtualKey.D,
				14	=> VirtualKey.E,
				3	=> VirtualKey.F,
				5	=> VirtualKey.G,
				4	=> VirtualKey.H,
				34	=> VirtualKey.I,
				38	=> VirtualKey.J,
				40	=> VirtualKey.K,
				37	=> VirtualKey.L,
				46	=> VirtualKey.M,
				45	=> VirtualKey.N,
				31	=> VirtualKey.O,
				35	=> VirtualKey.P,
				12	=> VirtualKey.Q,
				15	=> VirtualKey.R,
				1	=> VirtualKey.S,
				17	=> VirtualKey.T,
				32	=> VirtualKey.U,
				9	=> VirtualKey.V,
				13	=> VirtualKey.W,
				7	=> VirtualKey.X,
				16	=> VirtualKey.Y,
				6	=> VirtualKey.Z,

				// Those keys are not mapped in the VirtualKey enum by windows, however the event is still raised
				// WARNING: Those keys are only "LOCATION on keyboard" codes.
				//			This means that for instance the 187 is a '=' on a querty keyboard, while it's a '+' on an azerty.
				10	=> (VirtualKey)191, // § (Value observed on UWP 18362, fr-FR AZERTY US-int keyboard)
				50	=> (VirtualKey)192, // ` (Value observed on UWP 18362, fr-FR qwerty US-int keyboard)
				27	=> (VirtualKey)189, // - (Value observed on UWP 18362, fr-FR qwerty US-int keyboard)
				24	=> (VirtualKey)187, // = (Value observed on UWP 18362, fr-FR qwerty US-int keyboard)
				33	=> (VirtualKey)219, // [ (Value observed on UWP 18362, fr-FR qwerty US-int keyboard)
				30	=> (VirtualKey)221, // ] (Value observed on UWP 18362, fr-FR qwerty US-int keyboard)
				41	=> (VirtualKey)186, // ; (Value observed on UWP 18362, fr-FR qwerty US-int keyboard)
				39	=> (VirtualKey)222, // ' (Value observed on UWP 18362, fr-FR qwerty US-int keyboard)
				43	=> (VirtualKey)188, // , (Value observed on UWP 18362, fr-FR qwerty US-int keyboard)
				47	=> (VirtualKey)190, // . (Value observed on UWP 18362, fr-FR qwerty US-int keyboard)
				44	=> (VirtualKey)191, // / (Value observed on UWP 18362, fr-FR qwerty US-int keyboard)
				42	=> (VirtualKey)220, // \ (Value observed on UWP 18362, fr-FR qwerty US-int keyboard)

				// [Key|Number] Pad
				82	=> VirtualKey.NumberPad0,
				83	=> VirtualKey.NumberPad1,
				84	=> VirtualKey.NumberPad2,
				85	=> VirtualKey.NumberPad3,
				86	=> VirtualKey.NumberPad4,
				87	=> VirtualKey.NumberPad5,
				88	=> VirtualKey.NumberPad6,
				89	=> VirtualKey.NumberPad7,
				91	=> VirtualKey.NumberPad8,
				92	=> VirtualKey.NumberPad9,
				65	=> VirtualKey.Decimal,
				67	=> VirtualKey.Multiply,
				69	=> VirtualKey.Add,
				75	=> VirtualKey.Divide,
				78	=> VirtualKey.Subtract,
				81	=> VirtualKey.Enter, // =
				71	=> VirtualKey.Clear,
				76	=> VirtualKey.Enter,

				49	=> VirtualKey.Space,
				36	=> VirtualKey.Enter,
				48	=> VirtualKey.Tab,
				51	=> VirtualKey.Back,
				117	=> VirtualKey.Delete,
				// 52	=> VirtualKey.␊

				// Modifiers
				53	=> VirtualKey.Escape,
				55	=> VirtualKey.LeftWindows, // Command
				56	=> VirtualKey.Shift, // Left Shift
				57	=> VirtualKey.CapitalLock,
				58	=> VirtualKey.Menu, // Option, a.k.a. Alt
				59	=> VirtualKey.Control, // Left control
				60	=> VirtualKey.RightShift,
				61	=> VirtualKey.RightMenu, // Right option, a.k.a. Right Alt
				62	=> VirtualKey.RightControl,

				// Functions
				// 63	=> VirtualKey.fn
				122	=> VirtualKey.F1,
				120	=> VirtualKey.F2,
				99	=> VirtualKey.F3,
				118	=> VirtualKey.F4,
				96	=> VirtualKey.F5,
				97	=> VirtualKey.F6,
				98	=> VirtualKey.F7,
				100	=> VirtualKey.F8,
				101	=> VirtualKey.F9,
				109	=> VirtualKey.F10,
				103	=> VirtualKey.F11,
				111	=> VirtualKey.F12,
				105	=> VirtualKey.F13,
				107	=> VirtualKey.F14,
				113	=> VirtualKey.F15,
				106	=> VirtualKey.F16,
				64	=> VirtualKey.F17,
				79	=> VirtualKey.F18,
				80	=> VirtualKey.F19,
				90	=> VirtualKey.F20,

				// Volume (Those keys does not fire any event on UWP)
				// 72	=> VirtualKey. // Volume down
				// 73	=> VirtualKey. // Volume up
				// 74	=> VirtualKey. // Mute

				// Navigation
				114	=> VirtualKey.Insert,
				115	=> VirtualKey.Home,
				119	=> VirtualKey.End,
				116	=> VirtualKey.PageUp,
				121	=> VirtualKey.PageDown,
				123	=> VirtualKey.Left,
				124	=> VirtualKey.Right,
				125	=> VirtualKey.Down,
				126	=> VirtualKey.Up,

				_	=> VirtualKey.None,
			};
	}
}
