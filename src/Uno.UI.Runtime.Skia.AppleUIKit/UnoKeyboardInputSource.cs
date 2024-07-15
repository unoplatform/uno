using Foundation;
using UIKit;
using Windows.Foundation;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

internal sealed class UnoKeyboardInputSource : IUnoKeyboardInputSource
{
	public static UnoKeyboardInputSource Instance { get; } = new();

	private UnoKeyboardInputSource()
	{
	}

	public event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<object, KeyEventArgs>? KeyUp;

	public bool TryHandlePresses(NSSet<UIPress> presses, UIPressesEvent evt)
	{
		var handled = false;

#if __MACCATALYST__
		foreach (UIPress press in presses)
		{
			if (press.Key is null)
			{
				continue;
			}

			var virtualKey = VirtualKeyHelper.FromKeyCode(press.Key.KeyCode);
			var modifiers = VirtualKeyHelper.FromModifierFlags(press.Key.ModifierFlags);

			var args = new KeyEventArgs(
				"keyboard",
				virtualKey,
				modifiers,
				new CorePhysicalKeyStatus
				{
					ScanCode = (uint)press.Key.KeyCode,
					RepeatCount = 1,
				});

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"PressesEnded: {press.Key.KeyCode} -> {virtualKey}");
			}

			try
			{
				if (_ownerEvents is { })
				{
					_ownerEvents.RaiseKeyUp(args);

					var routerArgs = new KeyRoutedEventArgs(this, virtualKey, modifiers)
					{
						CanBubbleNatively = false
					};

					(FocusManager.GetFocusedElement() as FrameworkElement)?.RaiseEvent(UIElement.KeyUpEvent, routerArgs);

					handled = true;
				}
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}
#endif

		return handled;
	}
}
