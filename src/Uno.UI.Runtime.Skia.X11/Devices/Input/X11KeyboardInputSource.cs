using System;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace Uno.WinUI.Runtime.Skia.X11;

internal class X11KeyboardInputSource : IUnoKeyboardInputSource
{
	public event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<object, KeyEventArgs>? KeyUp;

	private X11XamlRootHost _host;

	public X11KeyboardInputSource(IXamlRootHost host)
	{
		if (host is not X11XamlRootHost)
		{
			throw new ArgumentException($"{nameof(host)} must be an X11 host instance");
		}

		_host = (X11XamlRootHost)host;
		_host.SetKeyboardSource(this);
	}

	internal void ProcessKeyboardEvent(XKeyEvent keyEvent, bool pressed)
	{
		unsafe
		{
			// unicode is at most 4 bytes
			var buffer = stackalloc byte[4];
			// TODO: Composing inputs https://wiki.debian.org/XCompose
			int nbytes = XLib.XLookupString(ref keyEvent, buffer, 4, out var keySym, IntPtr.Zero);

			// Resolve the VirtualKey from the keySym. XLookupString returns a modifier-aware keySym,
			// so e.g. Shift+0 gives XK_parenright instead of XK_0. Since VirtualKey should represent
			// the physical key (as on Windows), we fall back to the unshifted keySym when the
			// modifier-aware one is not in the mapping table.
			var virtualKey = X11KeyTransform.VirtualKeyFromKeySym(keySym);
			if (virtualKey == VirtualKey.None)
			{
				// XKeyEvent is a struct, so this copy is safe to mutate independently of keyEvent.
				var unshiftedKeyEvent = keyEvent;
				unshiftedKeyEvent.state &= ~XModifierMask.ShiftMask;
				// Pass num_bytes=0 because we only need the keySym output, not the translated string.
				XLib.XLookupString(ref unshiftedKeyEvent, buffer, 0, out var baseKeySym, IntPtr.Zero);
				virtualKey = X11KeyTransform.VirtualKeyFromKeySym(baseKeySym);
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"ProcessKeyboardEvent pressed={pressed}: {keyEvent.keycode} -> {virtualKey}");
			}

			// we make a call to libc's setlocale during startup, so buffer should be utf8
			var symbols = System.Text.Encoding.UTF8.GetString(buffer, nbytes);
			if (string.IsNullOrEmpty(symbols) || (symbols != "\r" && char.IsControl(symbols[0])))
			{
				symbols = null;
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"ProcessKeyboardEvent pressed={pressed}: {keyEvent.keycode} -> {virtualKey} utf8:{symbols?[0]}");
			}

			var args = new KeyEventArgs(
				"keyboard",
				virtualKey,
				X11XamlRootHost.XModifierMaskToVirtualKeyModifiers(keyEvent.state),
				new CorePhysicalKeyStatus
				{
					ScanCode = (uint)keyEvent.keycode,
					RepeatCount = 1,
				},
				unicodeKey: symbols?[0]);

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
}
