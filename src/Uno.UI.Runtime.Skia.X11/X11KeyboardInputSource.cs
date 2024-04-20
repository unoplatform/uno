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
			var buffer = stackalloc byte[4]; // unicode is at most 4 bytes
			int nbytes = XLib.XLookupString(ref keyEvent, buffer, 4, out var keySym, IntPtr.Zero);

			if (nbytes > 1)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Received keySym string with nbytes > 1, even though spec says it must be a single ASCII character.");
				}
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"ProcessKeyboardEvent pressed={pressed}: {keyEvent.keycode} -> {X11KeyTransform.VirtualKeyFromKeySym(keySym)}");
			}

			var symbols = System.Text.Encoding.UTF8.GetString(buffer, nbytes); // According to the docs, this should be ISO Latin-1 or ASCII
			if (string.IsNullOrEmpty(symbols) || (symbols != "\r" && char.IsControl(symbols[0])))
			{
				symbols = null;
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"ProcessKeyboardEvent pressed={pressed}: {keyEvent.keycode} -> {X11KeyTransform.VirtualKeyFromKeySym(keySym)} utf8:{symbols?[0]}");
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
