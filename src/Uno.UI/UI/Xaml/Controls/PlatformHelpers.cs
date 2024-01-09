using Microsoft.UI.Input;
using Uno;
using Windows.System;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Controls;

internal static class PlatformHelpers
{
	public static VirtualKeyModifiers GetKeyboardModifiers()
	{
		var pnKeyboardModifiers = VirtualKeyModifiers.None;

		var keyState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu);
		if (keyState.HasFlag(CoreVirtualKeyStates.Down))
		{
			pnKeyboardModifiers |= VirtualKeyModifiers.Menu;
		}

		keyState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
		if (keyState.HasFlag(CoreVirtualKeyStates.Down))
		{
			pnKeyboardModifiers |= VirtualKeyModifiers.Control;
		}

		keyState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
		if (keyState.HasFlag(CoreVirtualKeyStates.Down))
		{
			pnKeyboardModifiers |= VirtualKeyModifiers.Shift;
		}

		keyState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.RightWindows);
		if (keyState.HasFlag(CoreVirtualKeyStates.Down))
		{
			pnKeyboardModifiers |= VirtualKeyModifiers.Windows;
		}

		keyState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.LeftWindows);
		if (keyState.HasFlag(CoreVirtualKeyStates.Down))
		{
			pnKeyboardModifiers |= VirtualKeyModifiers.Windows;
		}

		return pnKeyboardModifiers;
	}

	public static void RequestInteractionSoundForElement(ElementSoundKind soundToPlay, DependencyObject element) =>
		ElementSoundPlayer.RequestInteractionSoundForElement(soundToPlay, element);

	public static ElementSoundMode GetEffectiveSoundMode(DependencyObject element) => 
		ElementSoundPlayer.GetEffectiveSoundMode(element);
}
