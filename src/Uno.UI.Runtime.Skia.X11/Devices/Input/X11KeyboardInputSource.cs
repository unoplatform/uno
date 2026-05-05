using System;
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

	// D-Bus IME backend (null if using XIM fallback)
	private IX11InputMethod? _dbusIme;
	private bool _dbusImeInitialized;

	internal bool IsDBusImeActive => _dbusIme?.IsEnabled == true;

	public X11KeyboardInputSource(IXamlRootHost host)
	{
		if (host is not X11XamlRootHost)
		{
			throw new ArgumentException($"{nameof(host)} must be an X11 host instance");
		}

		_host = (X11XamlRootHost)host;
		_host.SetKeyboardSource(this);

		InitDBusIme();
	}

	private void InitDBusIme()
	{
		if (_dbusImeInitialized)
		{
			return;
		}
		_dbusImeInitialized = true;

		_dbusIme = X11InputMethodDetector.DetectAndCreate();

		if (_dbusIme is not null)
		{
			// Wire Commit signal to the IME TextBox extension
			_dbusIme.Commit += OnDBusImeCommit;

			// Wire ForwardKey signal back into key processing
			_dbusIme.ForwardKey += OnDBusImeForwardKey;

			// Wire PreeditChanged signal
			_dbusIme.PreeditChanged += OnDBusImePreeditChanged;
		}
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
		// If D-Bus IME is active, route through it first
		if (_dbusIme?.IsEnabled == true)
		{
			ProcessKeyboardEventDBus(keyEvent, pressed);
			return;
		}

		// XIM fallback path
		ProcessKeyboardEventXIM(keyEvent, pressed);
	}

	private unsafe void ProcessKeyboardEventDBus(XKeyEvent keyEvent, bool pressed)
	{
		// Get keysym for virtual key mapping (D-Bus needs raw X11 keysym)
		var buffer = stackalloc byte[4];
		int nbytes = XLib.XLookupString(ref keyEvent, buffer, 4, out nint keySym, IntPtr.Zero);
		var text = nbytes > 0 ? System.Text.Encoding.UTF8.GetString(buffer, nbytes) : null;

		// Forward to D-Bus IME
		uint keyVal = (uint)(ulong)keySym;
		uint keyCode = (uint)keyEvent.keycode;
		uint state = (uint)keyEvent.state;

		bool handled;
		try
		{
			var task = _dbusIme!.HandleKeyEventAsync(keyVal, keyCode, state, !pressed);
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

		// IME did not handle — dispatch as normal key event
		string? symbols = null;
		if (!string.IsNullOrEmpty(text) && (text == "\r" || !char.IsControl(text[0])))
		{
			symbols = text;
		}

		DispatchKeyEvent(keyEvent, pressed, keySym, symbols);
	}

	private unsafe void ProcessKeyboardEventXIM(XKeyEvent keyEvent, bool pressed)
	{
		// Apply any pending spot location update from the UI thread.
		// This must happen on the event thread to avoid concurrent XIC access.
		X11ImeTextBoxExtension.Instance.FlushPendingSpotLocation();

		var xic = X11ImeTextBoxExtension.GetXicForWindow(keyEvent.window);

		string? symbols = null;
		nint keySym = 0;

		if (xic != IntPtr.Zero && pressed)
		{
			var buffer = stackalloc byte[64];
			int nbytes = XLib.Xutf8LookupString(xic, ref keyEvent, buffer, 64, out keySym, out var status);

			if (status == XLib.XBufferOverflow)
			{
				var largeBuffer = stackalloc byte[nbytes + 1];
				nbytes = XLib.Xutf8LookupString(xic, ref keyEvent, largeBuffer, nbytes + 1, out keySym, out status);
				buffer = largeBuffer;
			}

			var lookupText = nbytes > 0 ? System.Text.Encoding.UTF8.GetString(buffer, nbytes) : null;

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"XIM ProcessKeyboardEvent pressed={pressed}: keycode={keyEvent.keycode} keySym={keySym} status={status} nbytes={nbytes} text='{lookupText}' window=0x{keyEvent.window:X} xic=0x{xic:X}");
			}

			switch (status)
			{
				case XLib.XLookupBoth:
					if (!string.IsNullOrEmpty(lookupText))
					{
						symbols = lookupText;
					}
					break;

				case XLib.XLookupChars:
					if (!string.IsNullOrEmpty(lookupText) && !char.IsControl(lookupText[0]))
					{
						if (X11ImeTextBoxExtension.Instance.IsComposing)
						{
							var imeExtension = X11ImeTextBoxExtension.Instance;
							X11XamlRootHost.QueueAction(_host, () => imeExtension.OnCommittedText(lookupText));
							return;
						}

						symbols = lookupText;
						XLib.XLookupString(ref keyEvent, null, 0, out keySym, IntPtr.Zero);
					}
					else
					{
						return;
					}
					break;

				case XLib.XLookupKeySym:
					break;

				case XLib.XLookupNone:
					return;
			}
		}
		else
		{
			var buffer = stackalloc byte[4];
			int nbytes = XLib.XLookupString(ref keyEvent, buffer, 4, out keySym, IntPtr.Zero);

			var text = System.Text.Encoding.UTF8.GetString(buffer, nbytes);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"XIM ProcessKeyboardEvent pressed={pressed}: keycode={keyEvent.keycode} keySym={keySym} vk={X11KeyTransform.VirtualKeyFromKeySym(keySym)} text='{text}' nbytes={nbytes}");
			}
			if (!string.IsNullOrEmpty(text) && (text == "\r" || !char.IsControl(text[0])))
			{
				symbols = text;
			}
		}

		// Filter out control characters from symbols (except CR)
		if (symbols is not null && symbols != "\r" && symbols.Length > 0 && char.IsControl(symbols[0]))
		{
			symbols = null;
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
