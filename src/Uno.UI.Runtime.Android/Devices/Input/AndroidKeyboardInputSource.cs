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

namespace Uno.UI.Runtime.Android;

internal sealed class AndroidKeyboardInputSource : IUnoKeyboardInputSource
{
	internal AndroidKeyboardInputSource()
	{
	}

	public event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<object, KeyEventArgs>? KeyUp;
	event TypedEventHandler<object, CharacterReceivedEventArgs>? IUnoKeyboardInputSource.CharacterReceived { add { } remove { } }

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
			var unicodeChar = e.GetUnicodeChar(e.MetaState);

			// 0 means the key types nothing (Back, arrows, F keys...). As a '\0' it would raise CharacterReceived:
			// a ComboBox would type-ahead to its next item and a TextBox would insert it.
			var args = new KeyEventArgs("keyboard", virtualKey, modifiers, default(CorePhysicalKeyStatus)/*TODO*/, unicodeKey: unicodeChar == 0 ? null : (char)unicodeChar);
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
