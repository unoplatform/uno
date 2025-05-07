using System;
using Android.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Extensions;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class AndroidKeyboardInputSource : IUnoKeyboardInputSource
{
	public static AndroidKeyboardInputSource Instance { get; } = new();

	private AndroidKeyboardInputSource()
	{
	}

	public event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<object, KeyEventArgs>? KeyUp;

	internal bool OnNativeKeyEvent(KeyEvent e)
	{
		var virtualKey = VirtualKeyHelper.FromKeyCode(e!.KeyCode);
		var modifiers = VirtualKeyHelper.FromModifiers(e.Modifiers);

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"DispatchKeyEvent: {e.KeyCode} -> {virtualKey}");
		}

		try
		{
			var isDown = e.Action == KeyEventActions.Down;
			var args = new KeyEventArgs("keyboard", virtualKey, modifiers, default(CorePhysicalKeyStatus)/*TODO*/, unicodeKey: (char)e.GetUnicodeChar(e.MetaState));
			if (isDown)
			{
				KeyDown?.Invoke(this, args);
			}
			else
			{
				KeyUp?.Invoke(this, args);
			}

			return args.Handled;
		}
		catch (Exception ex)
		{
			Microsoft.UI.Xaml.Application.Current.RaiseRecoverableUnhandledException(ex);
			return false;
		}
	}
}
