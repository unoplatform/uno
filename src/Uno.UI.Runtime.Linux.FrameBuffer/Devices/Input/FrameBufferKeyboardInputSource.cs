#nullable enable

using System;
using System.Collections.Generic;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Native;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Uno.UI.Runtime;
using Uno.UI.Runtime.Linux.Input;
using static Uno.UI.Runtime.Native.LibInput;
using static Uno.UI.Runtime.Native.libinput_key;

namespace Uno.UI.Runtime.Linux.FrameBuffer;

internal class FrameBufferKeyboardInputSource : IUnoKeyboardInputSource
{
	private static FrameBufferKeyboardInputSource? _instance;

	public event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<object, KeyEventArgs>? KeyUp;
	event TypedEventHandler<object, CharacterReceivedEventArgs>? IUnoKeyboardInputSource.CharacterReceived { add { } remove { } }

	// libinput events arrive on the input thread and repeats on a timer thread.
	private readonly object _gate = new();
	private readonly XkbKeyboard? _xkb;
	private readonly XkbKeyRepeat _repeat;
	private readonly HashSet<libinput_key> _pressedKeys = new();
	// Keys consumed by a dead-key or Compose sequence, which raise no key events.
	private readonly HashSet<libinput_key> _consumedKeys = new();
	private readonly Dictionary<libinput_key, (VirtualKey VirtualKey, string? ComposedText)> _repeatable = new();
	private readonly IXamlRootHost? _host;

	public FrameBufferKeyboardInputSource(IXamlRootHost host, FramebufferHostBuilder.XKBKeymapParams keymapParams)
	{
		if (_instance is not null)
		{
			throw new InvalidOperationException($"{nameof(FrameBufferKeyboardInputSource)} is already created");
		}
		_instance = this;
		_host = host;
		_repeat = new XkbKeyRepeat(OnRepeat);

		try
		{
			_xkb = XkbKeyboard.FromNames(keymapParams.rules, keymapParams.model, keymapParams.layout, keymapParams.variant, keymapParams.options);
		}
		catch (Exception e)
		{
			this.LogError()?.Error($"libxkbcommon initialization failed with error '{e.Message}'. Keycode to keysym translations will be limited.");
		}
	}

	internal static FrameBufferKeyboardInputSource Instance => _instance!;

	internal void ProcessKeyboardEvent(IntPtr rawEvent, libinput_event_type rawEventType)
	{
		var rawKeyboardEvent = libinput_event_get_keyboard_event(rawEvent);

		if (rawKeyboardEvent != IntPtr.Zero)
		{
			var key = libinput_event_keyboard_get_key(rawKeyboardEvent);
			var state = libinput_event_keyboard_get_key_state(rawKeyboardEvent);

			if (state == libinput_key_state.Pressed)
			{
				OnKeyPressEvent(key);
			}
			else if (state == libinput_key_state.Released)
			{
				OnKeyReleaseEvent(key);
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"ProcessKeyboardEvent: Unsupported state {state} for {key}");
				}
			}
		}
	}

	private void OnKeyPressEvent(libinput_key key)
	{
		VirtualKey virtualKey;
		string? text = null;
		VirtualKeyModifiers modifiers;
		lock (_gate)
		{
			_pressedKeys.Add(key);
			virtualKey = ConvertToVirtualKey(key);
			if (_xkb is { } xkb)
			{
				var code = (uint)key;
				var keysym = xkb.GetKeysym(code);
				var keyText = xkb.GetUtf32(code);
				xkb.UpdateKey(code, down: true);
				if (LinuxKeyMap.FromKeysym(keysym) is not VirtualKey.None and var mapped)
				{
					virtualKey = mapped;
				}
				if (xkb.Compose(keysym, keyText, out var completeText) is not { } composed)
				{
					_consumedKeys.Add(key);
					_repeat.Stop();
					return;
				}
				var composedText = completeText ?? (composed != keyText ? ToText(composed) : null);
				text = composedText ?? ToText(composed);
				if (xkb.Repeats(code))
				{
					_repeatable[key] = (virtualKey, composedText);
					_repeat.Start(code);
				}
			}
			modifiers = GetCurrentModifiersState();
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"OnKeyPressEvent: {key} -> {virtualKey}");
		}

		RaiseKeyDown(virtualKey, modifiers, (uint)key, wasKeyDown: false, text);
	}

	private void RaiseKeyDown(VirtualKey virtualKey, VirtualKeyModifiers modifiers, uint code, bool wasKeyDown, string? text)
	{
		// A KeyDown carries one UTF-16 unit, so a longer composed string is sent unit by unit.
		for (var i = 0; i < Math.Max(1, text?.Length ?? 0); i++)
		{
			RaiseKeyEvent(KeyDown, new KeyEventArgs(
				"keyboard",
				virtualKey,
				modifiers,
				new CorePhysicalKeyStatus
				{
					ScanCode = code,
					RepeatCount = 1,
					WasKeyDown = wasKeyDown,
				},
				text is { Length: > 0 } ? text[i] : null));
		}
	}

	private static string? ToText(uint codePoint)
		=> codePoint is >= 32 and <= 0x10ffff and not (>= 0xd800 and <= 0xdfff) || codePoint == 13 ? char.ConvertFromUtf32((int)codePoint) : null;

	// Timer thread.
	private void OnRepeat(uint code)
	{
		VirtualKey virtualKey;
		VirtualKeyModifiers modifiers;
		string? text;
		lock (_gate)
		{
			var key = (libinput_key)code;
			if (_xkb is not { } xkb || _repeat.Key != code || !_repeatable.TryGetValue(key, out var repeated))
			{
				return;
			}
			// Translate with the modifiers held now, not at press time.
			virtualKey = LinuxKeyMap.FromKeysym(xkb.GetKeysym(code)) is not VirtualKey.None and var mapped ? mapped : repeated.VirtualKey;
			text = repeated.ComposedText ?? ToText(xkb.GetUtf32(code));
			modifiers = GetCurrentModifiersState();
		}

		RaiseKeyDown(virtualKey, modifiers, code, wasKeyDown: true, text);
	}

	private void OnKeyReleaseEvent(libinput_key key)
	{
		VirtualKey virtualKey;
		VirtualKeyModifiers modifiers;
		lock (_gate)
		{
			virtualKey = _repeatable.Remove(key, out var pressed) ? pressed.VirtualKey : ConvertToVirtualKey(key);
			if (_repeat.Key == (uint)key)
			{
				_repeat.Stop();
			}
			_xkb?.UpdateKey((uint)key, down: false);
			_pressedKeys.Remove(key);
			if (_consumedKeys.Remove(key))
			{
				return;
			}
			modifiers = GetCurrentModifiersState();
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"OnKeyReleaseEvent: {key} -> {virtualKey}");
		}

		var args = new KeyEventArgs(
			"keyboard",
			virtualKey,
			modifiers,
			new CorePhysicalKeyStatus
			{
				ScanCode = (uint)key,
				RepeatCount = 1,
			});

		RaiseKeyEvent(KeyUp, args);
	}

	private void RaiseKeyEvent(TypedEventHandler<object, KeyEventArgs>? raisePointerEvent, KeyEventArgs args)
	{
		if (_host?.RootElement is { } rootElement)
		{
			_ = rootElement.Dispatcher.RunAsync(
				CoreDispatcherPriority.High,
				() => raisePointerEvent?.Invoke(this, args));
		}
	}

	internal VirtualKeyModifiers GetCurrentModifiersState()
	{
		if (_xkb is { } xkb)
		{
			return xkb.Modifiers;
		}

		var modifiers = VirtualKeyModifiers.None;

		if (_pressedKeys.Contains(KEY_LEFTSHIFT) || _pressedKeys.Contains(KEY_RIGHTSHIFT))
		{
			modifiers |= VirtualKeyModifiers.Shift;
		}
		if (_pressedKeys.Contains(KEY_LEFTCTRL) || _pressedKeys.Contains(KEY_RIGHTCTRL))
		{
			modifiers |= VirtualKeyModifiers.Control;
		}
		if (_pressedKeys.Contains(KEY_LEFTALT) || _pressedKeys.Contains(KEY_RIGHTALT))
		{
			modifiers |= VirtualKeyModifiers.Menu;
		}
		if (_pressedKeys.Contains(KEY_LEFTMETA) || _pressedKeys.Contains(KEY_RIGHTMETA))
		{
			modifiers |= VirtualKeyModifiers.Windows;
		}

		return modifiers;
	}

	private VirtualKey ConvertToVirtualKey(libinput_key key)
		=> key switch
		{
			KEY_RESERVED => VirtualKey.None,
			KEY_ESC => VirtualKey.Escape,
			KEY_1 => VirtualKey.Number1,
			KEY_2 => VirtualKey.Number2,
			KEY_3 => VirtualKey.Number3,
			KEY_4 => VirtualKey.Number4,
			KEY_5 => VirtualKey.Number5,
			KEY_6 => VirtualKey.Number6,
			KEY_7 => VirtualKey.Number7,
			KEY_8 => VirtualKey.Number8,
			KEY_9 => VirtualKey.Number9,
			KEY_0 => VirtualKey.Number0,
			KEY_MINUS => VirtualKeyHelper.OemMinus,
			KEY_EQUAL => VirtualKeyHelper.OemPlus,
			KEY_BACKSPACE => VirtualKey.Back,
			KEY_TAB => VirtualKey.Tab,
			KEY_Q => VirtualKey.Q,
			KEY_W => VirtualKey.W,
			KEY_E => VirtualKey.E,
			KEY_R => VirtualKey.R,
			KEY_T => VirtualKey.T,
			KEY_Y => VirtualKey.Y,
			KEY_U => VirtualKey.U,
			KEY_I => VirtualKey.I,
			KEY_O => VirtualKey.O,
			KEY_P => VirtualKey.P,
			KEY_LEFTBRACE => VirtualKeyHelper.Oem4,
			KEY_RIGHTBRACE => VirtualKeyHelper.Oem6,
			KEY_ENTER => VirtualKey.Enter,
			KEY_LEFTCTRL => VirtualKey.LeftControl,
			KEY_A => VirtualKey.A,
			KEY_S => VirtualKey.S,
			KEY_D => VirtualKey.D,
			KEY_F => VirtualKey.F,
			KEY_G => VirtualKey.G,
			KEY_H => VirtualKey.H,
			KEY_J => VirtualKey.J,
			KEY_K => VirtualKey.K,
			KEY_L => VirtualKey.L,
			KEY_SEMICOLON => VirtualKeyHelper.Oem1,
			KEY_APOSTROPHE => VirtualKeyHelper.Oem7,
			KEY_GRAVE => VirtualKeyHelper.Oem3,
			KEY_LEFTSHIFT => VirtualKey.LeftShift,
			KEY_BACKSLASH => VirtualKeyHelper.Oem5,
			KEY_Z => VirtualKey.Z,
			KEY_X => VirtualKey.X,
			KEY_C => VirtualKey.C,
			KEY_V => VirtualKey.V,
			KEY_B => VirtualKey.B,
			KEY_N => VirtualKey.N,
			KEY_M => VirtualKey.M,
			KEY_COMMA => VirtualKeyHelper.OemComma,
			KEY_DOT => VirtualKeyHelper.OemPeriod,
			KEY_SLASH => VirtualKeyHelper.Oem2,
			KEY_RIGHTSHIFT => VirtualKey.RightShift,
			KEY_KPASTERISK => VirtualKey.Multiply,
			KEY_LEFTALT => VirtualKey.LeftMenu,
			KEY_SPACE => VirtualKey.Space,
			KEY_CAPSLOCK => VirtualKey.CapitalLock,
			KEY_F1 => VirtualKey.F1,
			KEY_F2 => VirtualKey.F2,
			KEY_F3 => VirtualKey.F3,
			KEY_F4 => VirtualKey.F4,
			KEY_F5 => VirtualKey.F5,
			KEY_F6 => VirtualKey.F6,
			KEY_F7 => VirtualKey.F7,
			KEY_F8 => VirtualKey.F8,
			KEY_F9 => VirtualKey.F9,
			KEY_F10 => VirtualKey.F10,
			KEY_NUMLOCK => VirtualKey.NumberKeyLock,
			KEY_SCROLLLOCK => VirtualKey.Scroll,
			KEY_KP7 => VirtualKey.NumberPad7,
			KEY_KP8 => VirtualKey.NumberPad8,
			KEY_KP9 => VirtualKey.NumberPad9,
			KEY_KPMINUS => VirtualKey.Subtract,
			KEY_KP4 => VirtualKey.NumberPad4,
			KEY_KP5 => VirtualKey.NumberPad5,
			KEY_KP6 => VirtualKey.NumberPad6,
			KEY_KPPLUS => VirtualKey.Add,
			KEY_KP1 => VirtualKey.NumberPad1,
			KEY_KP2 => VirtualKey.NumberPad2,
			KEY_KP3 => VirtualKey.NumberPad3,
			KEY_KP0 => VirtualKey.NumberPad0,
			KEY_KPDOT => VirtualKey.Decimal,

			KEY_ZENKAKUHANKAKU => VirtualKey.None,
			KEY_102ND => VirtualKeyHelper.Oem102,
			KEY_F11 => VirtualKey.F11,
			KEY_F12 => VirtualKey.F12,
			KEY_RO => VirtualKey.None,
			KEY_KATAKANA => VirtualKey.None,
			KEY_HIRAGANA => VirtualKey.None,
			KEY_HENKAN => VirtualKey.None,
			KEY_KATAKANAHIRAGANA => VirtualKey.None,
			KEY_MUHENKAN => VirtualKey.None,
			KEY_KPJPCOMMA => VirtualKey.None,
			KEY_KPENTER => VirtualKey.Enter,
			KEY_RIGHTCTRL => VirtualKey.RightControl,
			KEY_KPSLASH => VirtualKey.Divide,
			KEY_SYSRQ => VirtualKey.None,
			KEY_RIGHTALT => VirtualKey.RightMenu,
			KEY_LINEFEED => VirtualKey.None,
			KEY_HOME => VirtualKey.Home,
			KEY_UP => VirtualKey.Up,
			KEY_PAGEUP => VirtualKey.PageUp,
			KEY_LEFT => VirtualKey.Left,
			KEY_RIGHT => VirtualKey.Right,
			KEY_END => VirtualKey.End,
			KEY_DOWN => VirtualKey.Down,
			KEY_PAGEDOWN => VirtualKey.PageDown,
			KEY_INSERT => VirtualKey.Insert,
			KEY_DELETE => VirtualKey.Delete,
			KEY_MACRO => VirtualKey.None,
			KEY_MUTE => VirtualKeyHelper.VolumeMute,
			KEY_VOLUMEDOWN => VirtualKeyHelper.VolumeDown,
			KEY_VOLUMEUP => VirtualKeyHelper.VolumeUp,
			KEY_POWER => VirtualKey.None, /* SC System Power Down */
			KEY_KPEQUAL => VirtualKey.None,
			KEY_KPPLUSMINUS => VirtualKey.None,
			KEY_PAUSE => VirtualKey.Pause,
			KEY_SCALE => VirtualKey.None, /* AL Compiz Scale (Expose) */

			KEY_KPCOMMA => VirtualKey.None,
			KEY_HANGEUL => VirtualKey.None,
			KEY_HANJA => VirtualKey.None,
			KEY_YEN => VirtualKey.None,
			KEY_LEFTMETA => VirtualKey.LeftWindows,
			KEY_RIGHTMETA => VirtualKey.RightWindows,
			KEY_COMPOSE => VirtualKey.None,

			KEY_STOP => VirtualKey.None,  /* AC Stop */
			KEY_AGAIN => VirtualKey.None,
			KEY_PROPS => VirtualKey.None, /* AC Properties */
			KEY_UNDO => VirtualKey.None,  /* AC Undo */
			KEY_FRONT => VirtualKey.None,
			KEY_COPY => VirtualKey.None,  /* AC Copy */
			KEY_OPEN => VirtualKey.None,  /* AC Open */
			KEY_PASTE => VirtualKey.None, /* AC Paste */
			KEY_FIND => VirtualKey.None,  /* AC Search */
			KEY_CUT => VirtualKey.None,   /* AC Cut */
			KEY_HELP => VirtualKey.None,  /* AL Integrated Help Center */
			KEY_MENU => VirtualKey.None,  /* Menu (show menu) */
			KEY_CALC => VirtualKey.None,  /* AL Calculator */
			KEY_SETUP => VirtualKey.None,
			KEY_SLEEP => VirtualKey.Sleep, /* SC System Sleep */
			KEY_WAKEUP => VirtualKey.None,    /* System Wake Up */
			KEY_FILE => VirtualKey.None,  /* AL Local Machine Browser */
			KEY_SENDFILE => VirtualKey.None,
			KEY_DELETEFILE => VirtualKey.None,
			KEY_XFER => VirtualKey.None,
			KEY_PROG1 => VirtualKey.None,
			KEY_PROG2 => VirtualKey.None,
			KEY_WWW => VirtualKey.None,   /* AL Internet Browser */
			KEY_MSDOS => VirtualKey.None,
			KEY_COFFEE => VirtualKey.None,    /* AL Terminal Lock/Screensaver */
			KEY_ROTATE_DISPLAY => VirtualKey.None,    /* Display orientation for e.g. tablets */
			KEY_CYCLEWINDOWS => VirtualKey.None,
			KEY_MAIL => VirtualKey.None,
			KEY_BOOKMARKS => VirtualKey.None, /* AC Bookmarks */
			KEY_COMPUTER => VirtualKey.None,
			KEY_BACK => VirtualKey.GoBack,  /* AC Back */
			KEY_FORWARD => VirtualKey.GoForward,   /* AC Forward */
			KEY_CLOSECD => VirtualKey.None,
			KEY_EJECTCD => VirtualKey.None,
			KEY_EJECTCLOSECD => VirtualKey.None,
			KEY_NEXTSONG => VirtualKeyHelper.MediaNextTrack,
			KEY_PLAYPAUSE => VirtualKeyHelper.MediaPlayPause,
			KEY_PREVIOUSSONG => VirtualKeyHelper.MediaPrevTrack,
			KEY_STOPCD => VirtualKeyHelper.MediaStop,
			KEY_RECORD => VirtualKey.None,
			KEY_REWIND => VirtualKey.None,
			KEY_PHONE => VirtualKey.None, /* Media Select Telephone */
			KEY_ISO => VirtualKey.None,
			KEY_CONFIG => VirtualKey.None,    /* AL Consumer Control Configuration */
			KEY_HOMEPAGE => VirtualKey.GoHome,  /* AC Home */
			KEY_REFRESH => VirtualKey.Refresh,   /* AC Refresh */
			KEY_EXIT => VirtualKey.None,  /* AC Exit */
			KEY_MOVE => VirtualKey.None,
			KEY_EDIT => VirtualKey.None,
			KEY_SCROLLUP => VirtualKey.None,
			KEY_SCROLLDOWN => VirtualKey.None,
			KEY_KPLEFTPAREN => VirtualKey.None,
			KEY_KPRIGHTPAREN => VirtualKey.None,
			KEY_NEW => VirtualKey.None,   /* AC New */
			KEY_REDO => VirtualKey.None,  /* AC Redo/Repeat */

			KEY_F13 => VirtualKey.F13,
			KEY_F14 => VirtualKey.F14,
			KEY_F15 => VirtualKey.F15,
			KEY_F16 => VirtualKey.F16,
			KEY_F17 => VirtualKey.F17,
			KEY_F18 => VirtualKey.F18,
			KEY_F19 => VirtualKey.F19,
			KEY_F20 => VirtualKey.F20,
			KEY_F21 => VirtualKey.F21,
			KEY_F22 => VirtualKey.F22,
			KEY_F23 => VirtualKey.F23,
			KEY_F24 => VirtualKey.F24,

			KEY_PLAYCD => VirtualKey.None,
			KEY_PAUSECD => VirtualKey.None,
			KEY_PROG3 => VirtualKey.None,
			KEY_PROG4 => VirtualKey.None,
			KEY_DASHBOARD => VirtualKey.None, /* AL Dashboard */
			KEY_SUSPEND => VirtualKey.None,
			KEY_CLOSE => VirtualKey.None, /* AC Close */
			KEY_PLAY => VirtualKey.None,
			KEY_FASTFORWARD => VirtualKey.None,
			KEY_BASSBOOST => VirtualKey.None,
			KEY_PRINT => VirtualKey.None, /* AC Print */
			KEY_HP => VirtualKey.None,
			KEY_CAMERA => VirtualKey.None,
			KEY_SOUND => VirtualKey.None,
			KEY_QUESTION => VirtualKey.None,
			KEY_EMAIL => VirtualKey.None,
			KEY_CHAT => VirtualKey.None,
			KEY_SEARCH => VirtualKey.None,
			KEY_CONNECT => VirtualKey.None,
			KEY_FINANCE => VirtualKey.None,   /* AL Checkbook/Finance */
			KEY_SPORT => VirtualKey.None,
			KEY_SHOP => VirtualKey.None,
			KEY_ALTERASE => VirtualKey.None,
			KEY_CANCEL => VirtualKey.None,    /* AC Cancel */
			KEY_BRIGHTNESSDOWN => VirtualKey.None,
			KEY_BRIGHTNESSUP => VirtualKey.None,
			KEY_MEDIA => VirtualKey.None,

			KEY_SWITCHVIDEOMODE => VirtualKey.None,   /* Cycle between available video
											 =outputs (Monitor/LCD/TV-out/etc) */
			KEY_KBDILLUMTOGGLE => VirtualKey.None,
			KEY_KBDILLUMDOWN => VirtualKey.None,
			KEY_KBDILLUMUP => VirtualKey.None,

			KEY_SEND => VirtualKey.None,  /* AC Send */
			KEY_REPLY => VirtualKey.None, /* AC Reply */
			KEY_FORWARDMAIL => VirtualKey.None,   /* AC Forward Msg */
			KEY_SAVE => VirtualKey.None,  /* AC Save */
			KEY_DOCUMENTS => VirtualKey.None,

			KEY_BATTERY => VirtualKey.None,

			KEY_BLUETOOTH => VirtualKey.None,
			KEY_WLAN => VirtualKey.None,
			KEY_UWB => VirtualKey.None,

			KEY_UNKNOWN => VirtualKey.None,

			KEY_VIDEO_NEXT => VirtualKey.None,    /* drive next video source */
			KEY_VIDEO_PREV => VirtualKey.None,    /* drive previous video source */
			KEY_BRIGHTNESS_CYCLE => VirtualKey.None,  /* brightness up after max is min */
			KEY_BRIGHTNESS_AUTO => VirtualKey.None,   /* Set Auto Brightness=> VirtualKey.None, manual
										 			=brightness control is off
										 			=rely on ambient */
			KEY_DISPLAY_OFF => VirtualKey.None,   /* display device to off state */

			KEY_WWAN => VirtualKey.None,  /* Wireless WAN (LTE UMTS GSM etc.) */
			KEY_RFKILL => VirtualKey.None,    /* Key that controls all radios */

			KEY_MICMUTE => VirtualKey.None,   /* Mute / unmute the microphone */
			_ => VirtualKey.None,
		};
}
