#nullable enable

using System;
using Windows.Devices.Input;
using Windows.UI.Core;
using Uno.Extensions;
using Uno.Logging;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfApplication = System.Windows.Application;
using WpfWindow = System.Windows.Window;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Windows.UI.Input;
using MouseDevice = System.Windows.Input.MouseDevice;
using System.Reflection;
using Windows.System;
using Uno.UI.Skia.Platform.Extensions;
using Microsoft.Extensions.Logging;

namespace Uno.UI.Skia.Platform	
{
	partial class WpfCoreWindowExtension : ICoreWindowExtension
	{
		private void HostOnKeyDown(object sender, System.Windows.Input.KeyEventArgs args)
		{
			try
			{
				var virtualKey = ConvertKey(args.Key);

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"OnKeyPressEvent: {args.Key} -> {virtualKey}");
				}

				_ownerEvents.RaiseKeyDown(
					new Windows.UI.Core.KeyEventArgs(
						"keyboard",
						virtualKey,
						new CorePhysicalKeyStatus
						{
							ScanCode = (uint)args.SystemKey,
							RepeatCount = 1,
						}));
			}
			catch (Exception e)
			{
				Windows.UI.Xaml.Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}

		private void HostOnKeyUp(object sender, System.Windows.Input.KeyEventArgs args)
		{
			try
			{
				var virtualKey = ConvertKey(args.Key);

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"OnKeyPressEvent: {args.Key} -> {virtualKey}");
				}

				_ownerEvents.RaiseKeyUp(
					new Windows.UI.Core.KeyEventArgs(
						"keyboard",
						virtualKey,
						new CorePhysicalKeyStatus
						{
							ScanCode = (uint)args.SystemKey,
							RepeatCount = 1,
						}));
			}
			catch (Exception e)
			{
				Windows.UI.Xaml.Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}

		private VirtualKey ConvertKey(System.Windows.Input.Key key)
		{
			// In this function, commented out lines correspond to VirtualKeys not yet
			// mapped to their native counterparts. Uncomment and fix as needed.

			return key switch
			{
				System.Windows.Input.Key.None => VirtualKey.None,
				System.Windows.Input.Key.Cancel => VirtualKey.Cancel,
				System.Windows.Input.Key.Back => VirtualKey.Back,
				System.Windows.Input.Key.Tab => VirtualKey.Tab,
				//System.Windows.Input.Key.LineFeed => VirtualKey.LineFeed,
				System.Windows.Input.Key.Clear => VirtualKey.Clear,
				System.Windows.Input.Key.Return => VirtualKey.Enter,
				System.Windows.Input.Key.Pause => VirtualKey.Pause,
				// System.Windows.Input.Key.Capital => VirtualKey.CapitalLock,
				System.Windows.Input.Key.CapsLock => VirtualKey.CapitalLock,
				System.Windows.Input.Key.KanaMode => VirtualKey.Kana,
				System.Windows.Input.Key.JunjaMode => VirtualKey.Junja,
				System.Windows.Input.Key.FinalMode => VirtualKey.Final,
				System.Windows.Input.Key.HanjaMode => VirtualKey.Hanja,
				System.Windows.Input.Key.Escape => VirtualKey.Escape,
				//System.Windows.Input.Key.ImeConvert => VirtualKey.ImeConvert,
				//System.Windows.Input.Key.ImeNonConvert => VirtualKey.ImeNonConvert,
				//System.Windows.Input.Key.ImeAccept => VirtualKey.ImeAccept,
				//System.Windows.Input.Key.ImeModeChange => VirtualKey.ImeModeChange,
				System.Windows.Input.Key.Space => VirtualKey.Space,
				//System.Windows.Input.Key.Prior => VirtualKey.Prior,
				System.Windows.Input.Key.PageUp => VirtualKey.PageUp,
				//System.Windows.Input.Key.Next => VirtualKey.Next,
				System.Windows.Input.Key.PageDown => VirtualKey.PageDown,
				System.Windows.Input.Key.End => VirtualKey.End,
				System.Windows.Input.Key.Home => VirtualKey.Home,
				System.Windows.Input.Key.Left => VirtualKey.Left,
				System.Windows.Input.Key.Up => VirtualKey.Up,
				System.Windows.Input.Key.Right => VirtualKey.Right,
				System.Windows.Input.Key.Down => VirtualKey.Down,
				System.Windows.Input.Key.Select => VirtualKey.Select,
				System.Windows.Input.Key.Print => VirtualKey.Print,
				System.Windows.Input.Key.Execute => VirtualKey.Execute,
				System.Windows.Input.Key.Snapshot => VirtualKey.Snapshot,
				System.Windows.Input.Key.Insert => VirtualKey.Insert,
				System.Windows.Input.Key.Delete => VirtualKey.Delete,
				System.Windows.Input.Key.Help => VirtualKey.Help,
				System.Windows.Input.Key.D0 => VirtualKey.Number0,
				System.Windows.Input.Key.D1 => VirtualKey.Number1,
				System.Windows.Input.Key.D2 => VirtualKey.Number2,
				System.Windows.Input.Key.D3 => VirtualKey.Number3,
				System.Windows.Input.Key.D4 => VirtualKey.Number4,
				System.Windows.Input.Key.D5 => VirtualKey.Number5,
				System.Windows.Input.Key.D6 => VirtualKey.Number6,
				System.Windows.Input.Key.D7 => VirtualKey.Number7,
				System.Windows.Input.Key.D8 => VirtualKey.Number8,
				System.Windows.Input.Key.D9 => VirtualKey.Number9,
				System.Windows.Input.Key.A => VirtualKey.A,
				System.Windows.Input.Key.B => VirtualKey.B,
				System.Windows.Input.Key.C => VirtualKey.C,
				System.Windows.Input.Key.D => VirtualKey.D,
				System.Windows.Input.Key.E => VirtualKey.E,
				System.Windows.Input.Key.F => VirtualKey.F,
				System.Windows.Input.Key.G => VirtualKey.G,
				System.Windows.Input.Key.H => VirtualKey.H,
				System.Windows.Input.Key.I => VirtualKey.I,
				System.Windows.Input.Key.J => VirtualKey.J,
				System.Windows.Input.Key.K => VirtualKey.K,
				System.Windows.Input.Key.L => VirtualKey.L,
				System.Windows.Input.Key.M => VirtualKey.M,
				System.Windows.Input.Key.N => VirtualKey.N,
				System.Windows.Input.Key.O => VirtualKey.O,
				System.Windows.Input.Key.P => VirtualKey.P,
				System.Windows.Input.Key.Q => VirtualKey.Q,
				System.Windows.Input.Key.R => VirtualKey.R,
				System.Windows.Input.Key.S => VirtualKey.S,
				System.Windows.Input.Key.T => VirtualKey.T,
				System.Windows.Input.Key.U => VirtualKey.U,
				System.Windows.Input.Key.V => VirtualKey.V,
				System.Windows.Input.Key.W => VirtualKey.W,
				System.Windows.Input.Key.X => VirtualKey.X,
				System.Windows.Input.Key.Y => VirtualKey.Y,
				System.Windows.Input.Key.Z => VirtualKey.Z,
				System.Windows.Input.Key.LWin => VirtualKey.LeftWindows,
				System.Windows.Input.Key.RWin => VirtualKey.RightWindows,
				// System.Windows.Input.Key.Apps => VirtualKey.Apps,
				System.Windows.Input.Key.Sleep => VirtualKey.Sleep,
				System.Windows.Input.Key.NumPad0 => VirtualKey.NumberPad0,
				System.Windows.Input.Key.NumPad1 => VirtualKey.NumberPad1,
				System.Windows.Input.Key.NumPad2 => VirtualKey.NumberPad2,
				System.Windows.Input.Key.NumPad3 => VirtualKey.NumberPad3,
				System.Windows.Input.Key.NumPad4 => VirtualKey.NumberPad4,
				System.Windows.Input.Key.NumPad5 => VirtualKey.NumberPad5,
				System.Windows.Input.Key.NumPad6 => VirtualKey.NumberPad6,
				System.Windows.Input.Key.NumPad7 => VirtualKey.NumberPad7,
				System.Windows.Input.Key.NumPad8 => VirtualKey.NumberPad8,
				System.Windows.Input.Key.NumPad9 => VirtualKey.NumberPad9,
				System.Windows.Input.Key.Multiply => VirtualKey.Multiply,
				System.Windows.Input.Key.Add => VirtualKey.Add,
				System.Windows.Input.Key.Separator => VirtualKey.Separator,
				System.Windows.Input.Key.Subtract => VirtualKey.Subtract,
				System.Windows.Input.Key.Decimal => VirtualKey.Decimal,
				System.Windows.Input.Key.Divide => VirtualKey.Divide,
				System.Windows.Input.Key.F1 => VirtualKey.F1,
				System.Windows.Input.Key.F2 => VirtualKey.F2,
				System.Windows.Input.Key.F3 => VirtualKey.F3,
				System.Windows.Input.Key.F4 => VirtualKey.F4,
				System.Windows.Input.Key.F5 => VirtualKey.F5,
				System.Windows.Input.Key.F6 => VirtualKey.F6,
				System.Windows.Input.Key.F7 => VirtualKey.F7,
				System.Windows.Input.Key.F8 => VirtualKey.F8,
				System.Windows.Input.Key.F9 => VirtualKey.F9,
				System.Windows.Input.Key.F10 => VirtualKey.F10,
				System.Windows.Input.Key.F11 => VirtualKey.F11,
				System.Windows.Input.Key.F12 => VirtualKey.F12,
				System.Windows.Input.Key.F13 => VirtualKey.F13,
				System.Windows.Input.Key.F14 => VirtualKey.F14,
				System.Windows.Input.Key.F15 => VirtualKey.F15,
				System.Windows.Input.Key.F16 => VirtualKey.F16,
				System.Windows.Input.Key.F17 => VirtualKey.F17,
				System.Windows.Input.Key.F18 => VirtualKey.F18,
				System.Windows.Input.Key.F19 => VirtualKey.F19,
				System.Windows.Input.Key.F20 => VirtualKey.F20,
				System.Windows.Input.Key.F21 => VirtualKey.F21,
				System.Windows.Input.Key.F22 => VirtualKey.F22,
				System.Windows.Input.Key.F23 => VirtualKey.F23,
				System.Windows.Input.Key.F24 => VirtualKey.F24,
				System.Windows.Input.Key.NumLock => VirtualKey.NumberKeyLock,
				System.Windows.Input.Key.Scroll => VirtualKey.Scroll,
				System.Windows.Input.Key.LeftShift => VirtualKey.LeftShift,
				System.Windows.Input.Key.RightShift => VirtualKey.RightShift,
				System.Windows.Input.Key.LeftCtrl => VirtualKey.LeftControl,
				System.Windows.Input.Key.RightCtrl => VirtualKey.RightControl,
				System.Windows.Input.Key.LeftAlt => VirtualKey.LeftMenu,
				System.Windows.Input.Key.RightAlt => VirtualKey.RightMenu,
				// System.Windows.Input.Key.BrowserBack => VirtualKey.BrowserBack,
				// System.Windows.Input.Key.BrowserForward => VirtualKey.BrowserForward,
				// System.Windows.Input.Key.BrowserRefresh => VirtualKey.BrowserRefresh,
				// System.Windows.Input.Key.BrowserStop => VirtualKey.BrowserStop,
				// System.Windows.Input.Key.BrowserSearch => VirtualKey.BrowserSearch,
				// System.Windows.Input.Key.BrowserFavorites => VirtualKey.BrowserFavorites,
				// System.Windows.Input.Key.BrowserHome => VirtualKey.BrowserHome,
				// System.Windows.Input.Key.VolumeMute => VirtualKey.VolumeMute,
				// System.Windows.Input.Key.VolumeDown => VirtualKey.VolumeDown,
				// System.Windows.Input.Key.VolumeUp => VirtualKey.VolumeUp,
				// System.Windows.Input.Key.MediaNextTrack => VirtualKey.MediaNextTrack,
				// System.Windows.Input.Key.MediaPreviousTrack => VirtualKey.MediaPreviousTrack,
				// System.Windows.Input.Key.MediaStop => VirtualKey.MediaStop,
				// System.Windows.Input.Key.MediaPlayPause => VirtualKey.MediaPlayPause,
				// System.Windows.Input.Key.LaunchMail => VirtualKey.LaunchMail,
				// System.Windows.Input.Key.SelectMedia => VirtualKey.SelectMedia,
				// System.Windows.Input.Key.LaunchApplication1 => VirtualKey.LaunchApplication1,
				// System.Windows.Input.Key.LaunchApplication2 => VirtualKey.LaunchApplication2,
				// System.Windows.Input.Key.Oem1 => VirtualKey.Oem1,
				// System.Windows.Input.Key.OemSemicolon => VirtualKey.OemSemicolon,
				// System.Windows.Input.Key.OemPlus => VirtualKey.OemPlus,
				// System.Windows.Input.Key.OemComma => VirtualKey.OemComma,
				// System.Windows.Input.Key.OemMinus => VirtualKey.OemMinus,
				// System.Windows.Input.Key.OemPeriod => VirtualKey.OemPeriod,
				// System.Windows.Input.Key.Oem2 => VirtualKey.Oem2,
				// System.Windows.Input.Key.OemQuestion => VirtualKey.OemQuestion,
				// System.Windows.Input.Key.Oem3 => VirtualKey.Oem3,
				// System.Windows.Input.Key.OemTilde => VirtualKey.OemTilde,
				// System.Windows.Input.Key.AbntC1 => VirtualKey.AbntC1,
				// System.Windows.Input.Key.AbntC2 => VirtualKey.AbntC2,
				// System.Windows.Input.Key.Oem4 => VirtualKey.Oem4,
				// System.Windows.Input.Key.OemOpenBrackets => VirtualKey.OemOpenBrackets,
				// System.Windows.Input.Key.Oem5 => VirtualKey.Oem5,
				// System.Windows.Input.Key.OemPipe => VirtualKey.OemPipe,
				// System.Windows.Input.Key.Oem6 => VirtualKey.Oem6,
				// System.Windows.Input.Key.OemCloseBrackets => VirtualKey.OemCloseBrackets,
				// System.Windows.Input.Key.Oem7 => VirtualKey.Oem7,
				// System.Windows.Input.Key.OemQuotes => VirtualKey.OemQuotes,
				// System.Windows.Input.Key.Oem8 => VirtualKey.Oem8,
				// System.Windows.Input.Key.Oem102 => VirtualKey.Oem102,
				// System.Windows.Input.Key.OemBackslash => VirtualKey.OemBackslash,
				// System.Windows.Input.Key.ImeProcessed => VirtualKey.ImeProcessed,
				// System.Windows.Input.Key.System => VirtualKey.System,
				// System.Windows.Input.Key.OemAttn => VirtualKey.OemAttn,
				// System.Windows.Input.Key.DbeAlphanumeric => VirtualKey.DbeAlphanumeric,
				// System.Windows.Input.Key.OemFinish => VirtualKey.OemFinish,
				// System.Windows.Input.Key.DbeKatakana => VirtualKey.DbeKatakana,
				// System.Windows.Input.Key.OemCopy => VirtualKey.OemCopy,
				// System.Windows.Input.Key.DbeHiragana => VirtualKey.DbeHiragana,
				// System.Windows.Input.Key.OemAuto => VirtualKey.OemAuto,
				// System.Windows.Input.Key.DbeSbcsChar => VirtualKey.DbeSbcsChar,
				// System.Windows.Input.Key.OemEnlw => VirtualKey.OemEnlw,
				// System.Windows.Input.Key.DbeDbcsChar => VirtualKey.DbeDbcsChar,
				// System.Windows.Input.Key.OemBackTab => VirtualKey.OemBackTab,
				// System.Windows.Input.Key.DbeRoman => VirtualKey.DbeRoman,
				// System.Windows.Input.Key.Attn => VirtualKey.Attn,
				// System.Windows.Input.Key.DbeNoRoman => VirtualKey.DbeNoRoman,
				// System.Windows.Input.Key.CrSel => VirtualKey.CrSel,
				// System.Windows.Input.Key.DbeEnterWordRegisterMode => VirtualKey.DbeEnterWordRegisterMode,
				// System.Windows.Input.Key.ExSel => VirtualKey.ExSel,
				// System.Windows.Input.Key.DbeEnterImeConfigureMode => VirtualKey.DbeEnterImeConfigureMode,
				// System.Windows.Input.Key.EraseEof => VirtualKey.EraseEof,
				// System.Windows.Input.Key.DbeFlushString => VirtualKey.DbeFlushString,
				// System.Windows.Input.Key.Play => VirtualKey.Play,
				// System.Windows.Input.Key.DbeCodeInput => VirtualKey.DbeCodeInput,
				// System.Windows.Input.Key.Zoom => VirtualKey.Zoom,
				// System.Windows.Input.Key.DbeNoCodeInput => VirtualKey.DbeNoCodeInput,
				// System.Windows.Input.Key.NoName => VirtualKey.NoName,
				// System.Windows.Input.Key.DbeDetermineString => VirtualKey.DbeDetermineString,
				// System.Windows.Input.Key.Pa1 => VirtualKey.Pa1,
				// System.Windows.Input.Key.DbeEnterDialogConversionMode => VirtualKey.DbeEnterDialogConversionMode,
				// System.Windows.Input.Key.OemClear => VirtualKey.OemClear,
				// System.Windows.Input.Key.DeadCharProcessed => VirtualKey.DeadCharProcessed,

				_ => VirtualKey.None
			};
		}
	}
}
