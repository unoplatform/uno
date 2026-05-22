using System;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Windows.Foundation;
using Windows.UI.Core;

namespace Uno.WinUI.Runtime.Skia.X11;

internal class X11KeyboardInputSource : IUnoKeyboardInputSource
{
	public event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<object, KeyEventArgs>? KeyUp;

	private X11XamlRootHost _host;

	// D-Bus IME backend, populated asynchronously after we probe the session bus
	// for an actually-running ibus/fcitx service. Reads from the X11 event thread
	// see null until detection completes; in that window keys go through the plain
	// XLookupString path with no composition.
	private volatile IX11InputMethod? _dbusIme;

	public X11KeyboardInputSource(IXamlRootHost host)
	{
		if (host is not X11XamlRootHost)
		{
			throw new ArgumentException($"{nameof(host)} must be an X11 host instance");
		}

		_host = (X11XamlRootHost)host;
		_host.SetKeyboardSource(this);

		// Fire-and-forget: detection involves D-Bus probes which we don't want to
		// block the keyboard source constructor on. Keystrokes that arrive before
		// detection finishes go through the plain XLookupString path with no composition.
		_ = InitDBusImeAsync();
	}

	private async Task InitDBusImeAsync()
	{
		var ime = await X11InputMethodDetector.DetectAndCreateAsync();
		if (ime is null)
		{
			return;
		}

		ime.Commit += OnDBusImeCommit;
		ime.ForwardKey += OnDBusImeForwardKey;
		ime.PreeditChanged += OnDBusImePreeditChanged;

		_dbusIme = ime;
	}

	internal IX11InputMethod? GetDBusIme() => _dbusIme;

	private void OnDBusImeCommit(string text)
	{
		var imeExtension = X11ImeTextBoxExtension.Instance;
		X11XamlRootHost.QueueAction(_host, () => imeExtension.OnCommittedText(text));
	}

	private void OnDBusImeForwardKey(uint keyVal, uint keyCode, uint state)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"D-Bus IME ForwardKey: keyval=0x{keyVal:X} keycode={keyCode} state=0x{state:X}");
		}

		// IBus ReleaseMask is bit 30
		bool isRelease = (state & (1u << 30)) != 0;
		// Clear the release mask for virtual key mapping
		var cleanState = state & ~(1u << 30);

		var keySym = (nint)keyVal;
		var vk = X11KeyTransform.VirtualKeyFromKeySym(keySym);
		var modifiers = X11XamlRootHost.XModifierMaskToVirtualKeyModifiers((XModifierMask)cleanState);

		var args = new KeyEventArgs(
			"keyboard",
			vk,
			modifiers,
			new CorePhysicalKeyStatus
			{
				ScanCode = keyCode,
				RepeatCount = 1,
			},
			unicodeKey: null);

		X11XamlRootHost.QueueAction(_host, () =>
		{
			if (!isRelease)
			{
				KeyDown?.Invoke(this, args);
			}
			else
			{
				KeyUp?.Invoke(this, args);
			}
		});
	}

	private void OnDBusImePreeditChanged(string? preeditText, int cursorPos)
	{
		var imeExtension = X11ImeTextBoxExtension.Instance;
		X11XamlRootHost.QueueAction(_host, () =>
		{
			if (string.IsNullOrEmpty(preeditText))
			{
				// Preedit cleared — end composition if composing
				if (imeExtension.IsComposing)
				{
					imeExtension.OnPreeditChanged(null, 0);
				}
			}
			else
			{
				imeExtension.OnPreeditChanged(preeditText, cursorPos);
			}
		});
	}

	internal unsafe void ProcessKeyboardEvent(XKeyEvent keyEvent, bool pressed)
	{
		var buffer = stackalloc byte[4];
		int nbytes = XLib.XLookupString(ref keyEvent, buffer, 4, out nint keySym, IntPtr.Zero);
		var text = nbytes > 0 ? System.Text.Encoding.UTF8.GetString(buffer, nbytes) : null;

		if (_dbusIme?.IsEnabled == true)
		{
			uint keyVal = (uint)(ulong)keySym;
			uint keyCode = (uint)keyEvent.keycode;
			uint state = (uint)keyEvent.state;

			bool handled;
			try
			{
				var task = _dbusIme.HandleKeyEventAsync(keyVal, keyCode, state, !pressed);
				// Use a timeout to prevent stalling the UI/input thread if the IME service is slow.
				if (!task.Wait(TimeSpan.FromMilliseconds(100)))
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().Warn("D-Bus IME HandleKeyEvent timed out, treating as unhandled.");
					}
					handled = false;
				}
				else
				{
					handled = task.Result;
				}
			}
			catch (Exception ex)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"D-Bus IME HandleKeyEvent failed: {ex.Message}", ex);
				}
				handled = false;
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"D-Bus IME key: keyval=0x{keyVal:X} keycode={keyCode} state=0x{state:X} pressed={pressed} → handled={handled}");
			}

			if (handled)
			{
				// IME consumed the event — do not dispatch KeyDown/KeyUp
				return;
			}
		}
		else if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"ProcessKeyboardEvent pressed={pressed}: keycode={keyEvent.keycode} keySym={keySym} vk={X11KeyTransform.VirtualKeyFromKeySym(keySym)} text='{text}' nbytes={nbytes}");
		}

		string? symbols = null;
		if (!string.IsNullOrEmpty(text) && (text == "\r" || !char.IsControl(text[0])))
		{
			symbols = text;
		}

		DispatchKeyEvent(keyEvent, pressed, keySym, symbols);
	}

	private void DispatchKeyEvent(XKeyEvent keyEvent, bool pressed, nint keySym, string? symbols)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Dispatching {(pressed ? "KeyDown" : "KeyUp")}: vk={X11KeyTransform.VirtualKeyFromKeySym(keySym)} unicodeKey={(symbols?.Length > 0 ? symbols[0].ToString() : "null")}");
		}

		var args = new KeyEventArgs(
			"keyboard",
			X11KeyTransform.VirtualKeyFromKeySym(keySym),
			X11XamlRootHost.XModifierMaskToVirtualKeyModifiers(keyEvent.state),
			new CorePhysicalKeyStatus
			{
				ScanCode = (uint)keyEvent.keycode,
				RepeatCount = 1,
			},
			unicodeKey: symbols?.Length > 0 ? symbols[0] : null);

		X11XamlRootHost.QueueAction(_host, () =>
		{
			if (pressed)
			{
				KeyDown?.Invoke(this, args);
			}
			else
			{
				KeyUp?.Invoke(this, args);
			}
		});
	}
}
