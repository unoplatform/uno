// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\ModeContainer\ModeContainer.cpp, tag winui3/release/1.4.3, commit 685d2bf

#if __SKIA__
#nullable enable

using Windows.System;

namespace Microsoft.UI.Xaml.Input;

internal sealed partial class AKModeContainer
{
	/// <summary>
	/// Evaluate input message to see if we should activate access key navigation.
	/// Mode will change synchronously and fire IsActiveChanged event.
	/// Returns true if IsActive changed as a result of this input.
	/// </summary>
	private void EvaluateAccessKeyModeImpl(
		bool isAltKey,
		bool isKeyDown,
		bool isKeyUp,
		VirtualKey keyCode,
		bool isMenuKey,
		bool isNumpadInput,
		out bool isValid)
	{
		isValid = IsValidAccessKeyMessage(isAltKey, isKeyDown, isKeyUp, keyCode, isMenuKey, isNumpadInput);

		bool isAltAKMessage = IsAltAccessKeyMessage(isAltKey, isKeyDown, keyCode, isMenuKey);
		isValid |= isAltAKMessage;

		_akModeChanged = false;

		// If we have received a alt + keydown, this is recognized as a hotkey and should be processed
		if (isValid && isMenuKey && !_isActive)
		{
			_akModeChanged = true;
		}
		else if (isAltKey)
		{
			if (isKeyUp)
			{
				// We don't want to activate ak mode when using hotkeys
				if (isValid && !isMenuKey)
				{
					if ((!_isActive && _canEnterAccessKeyMode) || (_isActive && !_lockExitingAccessKeyModeOnAltUp))
					{
						_akModeChanged = true;
						_canEnterAccessKeyMode = false;
						SetIsActive(!_isActive);
					}
				}
			}
		}
	}

	/// <summary>
	/// This type of input is an access key pressed down with alt.
	/// Should treat this as a valid accesskey and invoke.
	/// </summary>
	private bool IsAltAccessKeyMessage(bool isAltKey, bool isKeyDown, VirtualKey keyCode, bool isMenuKey)
	{
		// When Alt then A is pressed, the first KeyDown message will contain isMenuKey==true with subsequent ones having this field set to false.
		// Therefore, this blocks repeatedly navigating down an access key hierarchy without using alt+AK without lifting the key and repressing it.
		return _isActive && isMenuKey && keyCode >= VirtualKey.Number0 && keyCode <= VirtualKey.Z && isKeyDown;
	}

	private bool IsValidAccessKeyMessage(bool isAltKey, bool isKeyDown, bool isKeyUp, VirtualKey keyCode, bool isMenuKey, bool isNumpadInput)
	{
		// All numeric access key messages are valid for both number and numpad keys when in Access Key mode.
		// When in hot-key mode, access keys are enabled only for number keys, but not numpad keys.
		// This follows the precedent set by Office's access keys, and helps disambiguate alt-numeric special
		// characters from access keys.
		return (_isActive && false /* IsChar - handled separately via CharacterReceived */) ||
				(isAltKey && isKeyUp) ||
				(keyCode == VirtualKey.Escape && isKeyDown && _isActive) ||
				(!isAltKey && !_isActive && isMenuKey && !isNumpadInput);
	}

	private void UpdateAKModeStateChangeLockout(in AccessKeyInputMessage message)
	{
		if (IsNakedAltKeyDown(in message) && !_lockEnteringAccessKeyModeUntilAltUp)
		{
			_canEnterAccessKeyMode = true; // Can only enter AK Mode when this is set to true.
			_lockExitingAccessKeyModeOnAltUp = false;
		}
		else if (!IsNakedAltKeyUp(in message) || _lockEnteringAccessKeyModeUntilAltUp)
		{
			// Any input after the alt down that is not an Alt up will cause canEnterAccessKeyMode to toggle to false.
			// This will prevent entering AK mode when using control+alt+delete, Alt+f4 etc.
			// Even if an alt key-up is received, without the corresponding key down no state change will occur.
			_canEnterAccessKeyMode = false;

			// If Alt was held down on the release of a key, toggle the latch so we do not enter AKmode on the alt release.
			if ((message.Modifiers & VirtualKeyModifiers.Menu) == VirtualKeyModifiers.Menu)
			{
				_lockEnteringAccessKeyModeUntilAltUp = true;
			}
			else if (message.Modifiers == 0)
			{
				// All modifiers released. Reset the lock (This also handles the case for alt up)
				_lockEnteringAccessKeyModeUntilAltUp = false;
				// In the case Alt+Ak is used to invoke an access key, we do not want the end of that message (Alt up)
				// to potentially cause an AK mode exit. Note: this does not preclude the invoke causing an exit, only the Alt Up.
				_lockExitingAccessKeyModeOnAltUp = true;
			}
		}
	}

	private static bool IsNakedAltKeyDown(in AccessKeyInputMessage message)
	{
		return message.IsKeyDown && message.IsAltKey && message.Modifiers == VirtualKeyModifiers.Menu;
	}

	private static bool IsNakedAltKeyUp(in AccessKeyInputMessage message)
	{
		return message.IsKeyUp && message.IsAltKey && message.Modifiers == 0;
	}
}
#endif
