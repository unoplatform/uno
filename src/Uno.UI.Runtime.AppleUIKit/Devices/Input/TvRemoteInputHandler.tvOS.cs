using System;
using System.Collections.Generic;
using Foundation;
using Microsoft.UI.Xaml;
using UIKit;
using Uno.UI.Xaml.Input;
using Windows.UI.Core;

namespace Uno.UI.Runtime.AppleUIKit;

/// <summary>
/// Turns Siri Remote presses and touch-surface swipes into key input for one window. Uno draws everything itself,
/// so the tvOS focus engine has no native views to move between and the remote has to drive Uno's own focus.
/// </summary>
internal sealed class TvRemoteInputHandler
{
	private readonly RemoteControlInputTranslator _translator;

	public TvRemoteInputHandler(UnoKeyboardInputSource keyboardSource)
		=> _translator = new(
			keyboardSource.RaiseKey,
			SystemNavigationManager.GetForCurrentView().RequestBack,
			e => Application.Current.RaiseRecoverableUnhandledException(e));

	internal void AttachSwipeRecognizers(UIView view)
	{
		AddSwipeRecognizer(view, UISwipeGestureRecognizerDirection.Up, RemoteControlButton.Up);
		AddSwipeRecognizer(view, UISwipeGestureRecognizerDirection.Down, RemoteControlButton.Down);
		AddSwipeRecognizer(view, UISwipeGestureRecognizerDirection.Left, RemoteControlButton.Left);
		AddSwipeRecognizer(view, UISwipeGestureRecognizerDirection.Right, RemoteControlButton.Right);
	}

	/// <returns>The presses left unhandled, to pass on to the system, or null when there are none.</returns>
	internal NSSet<UIPress>? OnPressesBegan(NSSet<UIPress> presses)
		=> Dispatch(presses, _translator.OnButtonDown);

	/// <returns>The releases of presses that were passed on to the system, or null when there are none.</returns>
	internal NSSet<UIPress>? OnPressesEnded(NSSet<UIPress> presses)
		=> Dispatch(presses, _translator.OnButtonUp);

	private static NSSet<UIPress>? Dispatch(NSSet<UIPress> presses, Func<RemoteControlButton, bool> handle)
	{
		List<UIPress>? unhandled = null;

		foreach (var press in presses)
		{
			var handled = ToRemoteControlButton(press.Type) is { } button && handle(button);
			if (!handled)
			{
				(unhandled ??= new()).Add(press);
			}
		}

		return unhandled is null ? null : new NSSet<UIPress>(unhandled.ToArray());
	}

	private void AddSwipeRecognizer(UIView view, UISwipeGestureRecognizerDirection direction, RemoteControlButton button)
		=> view.AddGestureRecognizer(new UISwipeGestureRecognizer(() => _translator.OnSwipe(button)) { Direction = direction });

	private static RemoteControlButton? ToRemoteControlButton(UIPressType type) => type switch
	{
		UIPressType.UpArrow => RemoteControlButton.Up,
		UIPressType.DownArrow => RemoteControlButton.Down,
		UIPressType.LeftArrow => RemoteControlButton.Left,
		UIPressType.RightArrow => RemoteControlButton.Right,
		UIPressType.Select => RemoteControlButton.Select,
		UIPressType.Menu => RemoteControlButton.Menu,
		UIPressType.PlayPause => RemoteControlButton.PlayPause,
		_ => null,
	};
}
