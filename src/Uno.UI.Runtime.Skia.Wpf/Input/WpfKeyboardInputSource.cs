using System.Windows.Input;
using System;
using System.Reflection;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Uno.UI.Helpers;
using Uno.UI.Hosting;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfUIElement = System.Windows.UIElement;
using WinUIKeyEventArgs = Windows.UI.Core.KeyEventArgs;

namespace Uno.UI.Runtime.Skia.Wpf.Input;

internal class WpfKeyboardInputSource : IUnoKeyboardInputSource
{
	public event TypedEventHandler<object, WinUIKeyEventArgs> KeyDown;
	public event TypedEventHandler<object, WinUIKeyEventArgs> KeyUp;

	public WpfKeyboardInputSource(IXamlRootHost host)
	{
		if (host is null) return;

		if (host is not WpfUIElement hostControl)
		{
			throw new ArgumentException($"{nameof(host)} must be a WPF Control instance", nameof(host));
		}

		hostControl.AddHandler(WpfUIElement.KeyUpEvent, (KeyEventHandler)HostOnKeyUp, true);
		hostControl.AddHandler(WpfUIElement.KeyDownEvent, (KeyEventHandler)HostOnKeyDown, true);
	}

	private void HostOnKeyDown(object sender, KeyEventArgs args)
	{
		try
		{
			var virtualKey = ConvertKey(args.Key);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"OnKeyPressEvent: {args.Key} -> {virtualKey}");
			}

			var scanCode = GetScanCode(args);

			KeyDown?.Invoke(this, new(
				"keyboard",
				virtualKey,
				GetKeyModifiers(args.KeyboardDevice.Modifiers),
				new CorePhysicalKeyStatus
				{
					ScanCode = scanCode,
					RepeatCount = 1,
				},
				KeyCodeToUnicode(scanCode)));
		}
		catch (Exception e)
		{
			Microsoft.UI.Xaml.Application.Current.RaiseRecoverableUnhandledException(e);
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

			var scanCode = GetScanCode(args);

			KeyUp?.Invoke(this, new(
				"keyboard",
				virtualKey,
				GetKeyModifiers(args.KeyboardDevice.Modifiers),
				new CorePhysicalKeyStatus
				{
					ScanCode = scanCode,
					RepeatCount = 1,
				},
				KeyCodeToUnicode(scanCode)));
		}
		catch (Exception e)
		{
			Microsoft.UI.Xaml.Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	// WPF doesn't expose the scancode, but it exists internally
	private static uint GetScanCode(KeyEventArgs args)
	{
		try
		{
			if (typeof(KeyEventArgs).GetProperty("ScanCode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) is { } propertyInfo)
			{
				return (uint)(int)propertyInfo.GetValue(args)!;
			}
			else
			{
				throw new PlatformNotSupportedException("Unable to get the ScanCode property from WPF. This likely means this WPF version is not compatible with Uno Platform, contact the developers for more information.");
			}
		}
		catch (Exception e)
		{
			if (typeof(WpfKeyboardInputSource).Log().IsEnabled(LogLevel.Error))
			{
				typeof(WpfKeyboardInputSource).Log().LogError("Unable to get ScanCode from WPF KeyEventArgs.", e);
			}

			throw;
		}
	}

	private static char? KeyCodeToUnicode(uint keyCode)
	{
		var result = InputHelper.WindowsScancodeToUnicode(keyCode);
		return result.Length > 0 ? result[0] : null; // TODO: supplementary code points
	}

	private static VirtualKeyModifiers GetKeyModifiers(ModifierKeys modifierKeys)
	{
		var modifiers = VirtualKeyModifiers.None;
		if (modifierKeys.HasFlag(ModifierKeys.Shift))
		{
			modifiers |= VirtualKeyModifiers.Shift;
		}
		if (modifierKeys.HasFlag(ModifierKeys.Control))
		{
			modifiers |= VirtualKeyModifiers.Control;
		}
		if (modifierKeys.HasFlag(ModifierKeys.Windows))
		{
			modifiers |= VirtualKeyModifiers.Windows;
		}
		return modifiers;
	}

	private VirtualKey ConvertKey(Key key)
	{
		// In this function, commented out lines correspond to VirtualKeys not yet
		// mapped to their native counterparts. Uncomment and fix as needed.

		return key switch
		{
			Key.None => VirtualKey.None,
			Key.Cancel => VirtualKey.Cancel,
			Key.Back => VirtualKey.Back,
			Key.Tab => VirtualKey.Tab,
			//System.Windows.Input.Key.LineFeed => VirtualKey.LineFeed,
			Key.Clear => VirtualKey.Clear,
			Key.Return => VirtualKey.Enter,
			Key.Pause => VirtualKey.Pause,
			// System.Windows.Input.Key.Capital => VirtualKey.CapitalLock,
			Key.CapsLock => VirtualKey.CapitalLock,
			Key.KanaMode => VirtualKey.Kana,
			Key.JunjaMode => VirtualKey.Junja,
			Key.FinalMode => VirtualKey.Final,
			Key.HanjaMode => VirtualKey.Hanja,
			Key.Escape => VirtualKey.Escape,
			//System.Windows.Input.Key.ImeConvert => VirtualKey.ImeConvert,
			//System.Windows.Input.Key.ImeNonConvert => VirtualKey.ImeNonConvert,
			//System.Windows.Input.Key.ImeAccept => VirtualKey.ImeAccept,
			//System.Windows.Input.Key.ImeModeChange => VirtualKey.ImeModeChange,
			Key.Space => VirtualKey.Space,
			//System.Windows.Input.Key.Prior => VirtualKey.Prior,
			Key.PageUp => VirtualKey.PageUp,
			//System.Windows.Input.Key.Next => VirtualKey.Next,
			Key.PageDown => VirtualKey.PageDown,
			Key.End => VirtualKey.End,
			Key.Home => VirtualKey.Home,
			Key.Left => VirtualKey.Left,
			Key.Up => VirtualKey.Up,
			Key.Right => VirtualKey.Right,
			Key.Down => VirtualKey.Down,
			Key.Select => VirtualKey.Select,
			Key.Print => VirtualKey.Print,
			Key.Execute => VirtualKey.Execute,
			Key.Snapshot => VirtualKey.Snapshot,
			Key.Insert => VirtualKey.Insert,
			Key.Delete => VirtualKey.Delete,
			Key.Help => VirtualKey.Help,
			Key.D0 => VirtualKey.Number0,
			Key.D1 => VirtualKey.Number1,
			Key.D2 => VirtualKey.Number2,
			Key.D3 => VirtualKey.Number3,
			Key.D4 => VirtualKey.Number4,
			Key.D5 => VirtualKey.Number5,
			Key.D6 => VirtualKey.Number6,
			Key.D7 => VirtualKey.Number7,
			Key.D8 => VirtualKey.Number8,
			Key.D9 => VirtualKey.Number9,
			Key.A => VirtualKey.A,
			Key.B => VirtualKey.B,
			Key.C => VirtualKey.C,
			Key.D => VirtualKey.D,
			Key.E => VirtualKey.E,
			Key.F => VirtualKey.F,
			Key.G => VirtualKey.G,
			Key.H => VirtualKey.H,
			Key.I => VirtualKey.I,
			Key.J => VirtualKey.J,
			Key.K => VirtualKey.K,
			Key.L => VirtualKey.L,
			Key.M => VirtualKey.M,
			Key.N => VirtualKey.N,
			Key.O => VirtualKey.O,
			Key.P => VirtualKey.P,
			Key.Q => VirtualKey.Q,
			Key.R => VirtualKey.R,
			Key.S => VirtualKey.S,
			Key.T => VirtualKey.T,
			Key.U => VirtualKey.U,
			Key.V => VirtualKey.V,
			Key.W => VirtualKey.W,
			Key.X => VirtualKey.X,
			Key.Y => VirtualKey.Y,
			Key.Z => VirtualKey.Z,
			Key.LWin => VirtualKey.LeftWindows,
			Key.RWin => VirtualKey.RightWindows,
			// System.Windows.Input.Key.Apps => VirtualKey.Apps,
			Key.Sleep => VirtualKey.Sleep,
			Key.NumPad0 => VirtualKey.NumberPad0,
			Key.NumPad1 => VirtualKey.NumberPad1,
			Key.NumPad2 => VirtualKey.NumberPad2,
			Key.NumPad3 => VirtualKey.NumberPad3,
			Key.NumPad4 => VirtualKey.NumberPad4,
			Key.NumPad5 => VirtualKey.NumberPad5,
			Key.NumPad6 => VirtualKey.NumberPad6,
			Key.NumPad7 => VirtualKey.NumberPad7,
			Key.NumPad8 => VirtualKey.NumberPad8,
			Key.NumPad9 => VirtualKey.NumberPad9,
			Key.Multiply => VirtualKey.Multiply,
			Key.Add => VirtualKey.Add,
			Key.Separator => VirtualKey.Separator,
			Key.Subtract => VirtualKey.Subtract,
			Key.Decimal => VirtualKey.Decimal,
			Key.Divide => VirtualKey.Divide,
			Key.F1 => VirtualKey.F1,
			Key.F2 => VirtualKey.F2,
			Key.F3 => VirtualKey.F3,
			Key.F4 => VirtualKey.F4,
			Key.F5 => VirtualKey.F5,
			Key.F6 => VirtualKey.F6,
			Key.F7 => VirtualKey.F7,
			Key.F8 => VirtualKey.F8,
			Key.F9 => VirtualKey.F9,
			Key.F10 => VirtualKey.F10,
			Key.F11 => VirtualKey.F11,
			Key.F12 => VirtualKey.F12,
			Key.F13 => VirtualKey.F13,
			Key.F14 => VirtualKey.F14,
			Key.F15 => VirtualKey.F15,
			Key.F16 => VirtualKey.F16,
			Key.F17 => VirtualKey.F17,
			Key.F18 => VirtualKey.F18,
			Key.F19 => VirtualKey.F19,
			Key.F20 => VirtualKey.F20,
			Key.F21 => VirtualKey.F21,
			Key.F22 => VirtualKey.F22,
			Key.F23 => VirtualKey.F23,
			Key.F24 => VirtualKey.F24,
			Key.NumLock => VirtualKey.NumberKeyLock,
			Key.Scroll => VirtualKey.Scroll,
			Key.LeftShift => VirtualKey.LeftShift,
			Key.RightShift => VirtualKey.RightShift,
			Key.LeftCtrl => VirtualKey.LeftControl,
			Key.RightCtrl => VirtualKey.RightControl,
			Key.LeftAlt => VirtualKey.LeftMenu,
			Key.RightAlt => VirtualKey.RightMenu,
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
