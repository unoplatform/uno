using Foundation;
using UIKit;
using Windows.Foundation;
using Windows.UI.Core;
using Uno.UI.Runtime.Skia.AppleUIKit.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using System;

#if __MACCATALYST__
using Windows.System;
using Uno.Foundation.Logging;
#endif

namespace Uno.UI.Runtime.Skia.AppleUIKit;

internal sealed class UnoKeyboardInputSource : IUnoKeyboardInputSource
{
	public static UnoKeyboardInputSource Instance { get; } = new();

	private UnoKeyboardInputSource()
	{
	}
#pragma warning disable CS0067
	public event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<object, KeyEventArgs>? KeyUp;
#pragma warning restore CS0067

	public bool TryHandlePresses(NSSet<UIPress> presses, UIPressesEvent evt, AppleUIKitWindow window)
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
				if (window.OwnerEvents is { } ownerEvents)
				{
					ownerEvents.RaiseKeyUp(args);

					var xamlRoot = Window.InitialWindow?.Content?.XamlRoot;
					if (xamlRoot is not null && FocusManager.GetFocusedElement(xamlRoot) is FrameworkElement fe)
					{
						var routerArgs = new KeyRoutedEventArgs(this, virtualKey, modifiers)
						{
							CanBubbleNatively = false
						};
						fe.RaiseEvent(UIElement.KeyUpEvent, routerArgs);
					}

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
