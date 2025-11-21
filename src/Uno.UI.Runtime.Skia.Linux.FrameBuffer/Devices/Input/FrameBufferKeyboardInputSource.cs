#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Native;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Uno.UI.Runtime.Skia;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.Devices.Input;
using static Uno.UI.Runtime.Skia.Native.LibInput;
using static Uno.UI.Runtime.Skia.Native.libinput_key;

namespace Uno.WinUI.Runtime.Skia.Linux.FrameBuffer;

internal class FrameBufferKeyboardInputSource : IUnoKeyboardInputSource
{
	private static FrameBufferKeyboardInputSource? _instance;

	public event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<object, KeyEventArgs>? KeyUp;

	private readonly IntPtr _xkbState;
	private HashSet<libinput_key> _pressedKeys = new HashSet<libinput_key>();
	private readonly IXamlRootHost? _host;

	public unsafe FrameBufferKeyboardInputSource(IXamlRootHost host, FramebufferHostBuilder.XKBKeymapParams keymapParams)
	{
		if (_instance is not null)
		{
			throw new InvalidOperationException($"{nameof(FrameBufferKeyboardInputSource)} is already created");
		}
		_instance = this;
		_host = host;

		try
		{
			var context = LibXKBCommon.xkb_context_new(LibXKBCommon.xkb_context_flags.XKB_CONTEXT_NO_FLAGS);
			if (context == IntPtr.Zero)
			{
				throw new InvalidOperationException($"{nameof(LibXKBCommon.xkb_context_new)} failed and returned null.");
			}

			using var ruleNames = new LibXKBCommon.xkb_rule_names(keymapParams);
			var keymap = LibXKBCommon.xkb_keymap_new_from_names(context, &ruleNames, LibXKBCommon.xkb_keymap_compile_flags.XKB_KEYMAP_COMPILE_NO_FLAGS);
			if (keymap == IntPtr.Zero)
			{
				throw new InvalidOperationException($"{nameof(LibXKBCommon.xkb_keymap_new_from_names)} failed and returned null.");
			}

			_xkbState = LibXKBCommon.xkb_state_new(keymap);
			if (keymap == IntPtr.Zero)
			{
				throw new InvalidOperationException($"{nameof(LibXKBCommon.xkb_state_new)} failed and returned null.");
			}
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

	private unsafe void OnKeyPressEvent(libinput_key key)
	{
		char? unicodeKey = default;
		if (_xkbState != IntPtr.Zero)
		{
			// https://github.com/xkbcommon/libxkbcommon/blob/master/test/state.c#L34
			var keycode = (uint)key + /* EVDEV_OFFSET */ 8;
			var keysym = LibXKBCommon.xkb_state_key_get_one_sym(_xkbState, keycode);
			if (keysym == /* XKB_KEY_NoSymbol */ 0)
			{
				this.LogError()?.Error($"{nameof(LibXKBCommon.xkb_state_key_get_one_sym)} failed to translate keycode {keycode} to a keysym");
			}
			if (this.Log().IsTraceEnabled())
			{
				var keysymName = stackalloc byte[64];
				var size = LibXKBCommon.xkb_keysym_get_name(keysym, keysymName, 64);
				if (size == -1)
				{
					this.LogError()?.Error($"{nameof(LibXKBCommon.xkb_keysym_get_name)} failed to translate keysym {keysym} to a string representation.");
				}
				else
				{
					var name = Marshal.PtrToStringAnsi((IntPtr)keysymName);
					this.LogTrace()?.Trace($"{nameof(LibXKBCommon.xkb_keysym_get_name)} translated keycode {keycode} to '{name}'");
				}
			}
			var utf8Buffersize = LibXKBCommon.xkb_state_key_get_utf8(_xkbState, keycode, null, 0) + 1;
			if (utf8Buffersize <= 1)
			{
				this.LogError()?.Error($"{nameof(LibXKBCommon.xkb_state_key_get_utf8)} failed to translate keycode {keycode} to a utf8 char.");
			}
			else
			{
				var utf8Buffer = stackalloc byte[utf8Buffersize];
				_ = LibXKBCommon.xkb_state_key_get_utf8(_xkbState, keycode, utf8Buffer, utf8Buffersize);
				var utf8String = Marshal.PtrToStringAnsi((IntPtr)utf8Buffer)!;
				unicodeKey = utf8String[0];
			}

			_ = LibXKBCommon.xkb_state_update_key(_xkbState, keycode, LibXKBCommon.xkb_key_direction.XKB_KEY_DOWN);
		}
		var virtualKey = ConvertToVirtualKey(key);

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"OnKeyPressEvent: {key} -> {virtualKey}");
		}

		_pressedKeys.Add(key);

		var args = new KeyEventArgs(
			"keyboard",
			virtualKey,
			GetCurrentModifiersState(),
			new CorePhysicalKeyStatus
			{
				ScanCode = (uint)key,
				RepeatCount = 1,
			},
			unicodeKey);

		RaiseKeyEvent(KeyDown, args);
	}

	private void OnKeyReleaseEvent(libinput_key key)
	{
		if (_xkbState != IntPtr.Zero)
		{
			var keycode = (uint)key + /* EVDEV_OFFSET */ 8;
			_ = LibXKBCommon.xkb_state_update_key(_xkbState, keycode, LibXKBCommon.xkb_key_direction.XKB_KEY_UP);
		}

		var virtualKey = ConvertToVirtualKey(key);

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"OnKeyReleaseEvent: {key} -> {virtualKey}");
		}

		_pressedKeys.Remove(key);

		var args = new KeyEventArgs(
			"keyboard",
			virtualKey,
			GetCurrentModifiersState(),
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
			KEY_MINUS => VirtualKey.Subtract,
			KEY_EQUAL => VirtualKey.None,
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
			KEY_LEFTBRACE => VirtualKey.None,
			KEY_RIGHTBRACE => VirtualKey.None,
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
			KEY_SEMICOLON => VirtualKey.None,
			KEY_APOSTROPHE => VirtualKey.None,
			KEY_GRAVE => VirtualKey.None,
			KEY_LEFTSHIFT => VirtualKey.LeftShift,
			KEY_BACKSLASH => VirtualKey.None,
			KEY_Z => VirtualKey.Z,
			KEY_X => VirtualKey.X,
			KEY_C => VirtualKey.C,
			KEY_V => VirtualKey.V,
			KEY_B => VirtualKey.B,
			KEY_N => VirtualKey.N,
			KEY_M => VirtualKey.M,
			KEY_COMMA => VirtualKey.None,
			KEY_DOT => VirtualKey.None,
			KEY_SLASH => VirtualKey.None,
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
			KEY_KPPLUS => VirtualKey.None,
			KEY_KP1 => VirtualKey.NumberPad1,
			KEY_KP2 => VirtualKey.NumberPad2,
			KEY_KP3 => VirtualKey.NumberPad3,
			KEY_KP0 => VirtualKey.NumberPad0,
			KEY_KPDOT => VirtualKey.Separator,

			KEY_ZENKAKUHANKAKU => VirtualKey.None,
			KEY_102ND => VirtualKey.None,
			KEY_F11 => VirtualKey.F11,
			KEY_F12 => VirtualKey.F12,
			KEY_RO => VirtualKey.None,
			KEY_KATAKANA => VirtualKey.None,
			KEY_HIRAGANA => VirtualKey.None,
			KEY_HENKAN => VirtualKey.None,
			KEY_KATAKANAHIRAGANA => VirtualKey.None,
			KEY_MUHENKAN => VirtualKey.None,
			KEY_KPJPCOMMA => VirtualKey.None,
			KEY_KPENTER => VirtualKey.None,
			KEY_RIGHTCTRL => VirtualKey.RightControl,
			KEY_KPSLASH => VirtualKey.None,
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
			KEY_MUTE => VirtualKey.None,
			KEY_VOLUMEDOWN => VirtualKey.None,
			KEY_VOLUMEUP => VirtualKey.None,
			KEY_POWER => VirtualKey.None, /* SC System Power Down */
			KEY_KPEQUAL => VirtualKey.None,
			KEY_KPPLUSMINUS => VirtualKey.None,
			KEY_PAUSE => VirtualKey.None,
			KEY_SCALE => VirtualKey.None, /* AL Compiz Scale (Expose) */

			KEY_KPCOMMA => VirtualKey.None,
			KEY_HANGEUL => VirtualKey.None,
			KEY_HANJA => VirtualKey.None,
			KEY_YEN => VirtualKey.None,
			KEY_LEFTMETA => VirtualKey.None,
			KEY_RIGHTMETA => VirtualKey.None,
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
			KEY_SLEEP => VirtualKey.None, /* SC System Sleep */
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
			KEY_BACK => VirtualKey.Back,  /* AC Back */
			KEY_FORWARD => VirtualKey.GoForward,   /* AC Forward */
			KEY_CLOSECD => VirtualKey.None,
			KEY_EJECTCD => VirtualKey.None,
			KEY_EJECTCLOSECD => VirtualKey.None,
			KEY_NEXTSONG => VirtualKey.None,
			KEY_PLAYPAUSE => VirtualKey.None,
			KEY_PREVIOUSSONG => VirtualKey.None,
			KEY_STOPCD => VirtualKey.None,
			KEY_RECORD => VirtualKey.None,
			KEY_REWIND => VirtualKey.None,
			KEY_PHONE => VirtualKey.None, /* Media Select Telephone */
			KEY_ISO => VirtualKey.None,
			KEY_CONFIG => VirtualKey.None,    /* AL Consumer Control Configuration */
			KEY_HOMEPAGE => VirtualKey.None,  /* AC Home */
			KEY_REFRESH => VirtualKey.None,   /* AC Refresh */
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
