// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\ModeContainer.h, tag winui3/release/1.5.3

#nullable enable

using System;
using Windows.System;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Xaml.Core;

namespace Uno.UI.Xaml.Input.AccessKeys;

/// <summary>
/// Manages the state of access key mode (whether Alt-key navigation is active).
/// Evaluates input to determine when to enter/exit access key mode.
/// </summary>
internal class AKModeContainer
{
	private bool _isActive;
	private bool _akModeChanged;
	private bool _forceQuit;
	private bool _canEnterAccessKeyMode; // Only want to enter AK mode when an alt key was pressed and released without other key input. This bool toggles to false whenever input that should disallow AK mode is entered.
	private bool _lockEnteringAccessKeyModeUntilAltUp;
	private bool _lockExitingAccessKeyModeOnAltUp;

	private FocusManager? _focusManager;

	/// <summary>
	/// Event raised when IsActive changes.
	/// </summary>
	internal event EventHandler? IsActiveChanged;

	internal AKModeContainer()
	{
		_isActive = false;
		_forceQuit = false;
		_akModeChanged = false;
		_canEnterAccessKeyMode = false;
		_lockEnteringAccessKeyModeUntilAltUp = false;
		_lockExitingAccessKeyModeOnAltUp = false;
	}

	/// <summary>
	/// Sets the focus manager for access key mode.
	/// </summary>
	internal void SetFocusManager(FocusManager? focusManager)
	{
		_focusManager = focusManager;
	}

	/// <summary>
	/// Sets whether access key mode is active.
	/// </summary>
	internal void SetIsActive(bool newValue)
	{
		if (GetIsActive() != newValue)
		{
			// AK_TRACE(L"AK> SetIsActive from %d to %d\n", _isActive, newValue);
			_isActive = newValue;
			OnIsActiveChanged();
		}
	}

	/// <summary>
	/// Gets whether access key mode is active.
	/// </summary>
	internal bool GetIsActive() => _isActive;

	/// <summary>
	/// Returns true if access key mode changed during the last input evaluation.
	/// </summary>
	internal bool HasAKModeChanged() => _akModeChanged;

	/// <summary>
	/// Returns true if access key mode should be forcibly exited.
	/// </summary>
	internal bool ShouldForciblyExitAKMode() => _forceQuit;

	/// <summary>
	/// Evaluates a key event to see if we should activate/deactivate access key navigation.
	/// Mode will change synchronously and fire IsActiveChanged event.
	/// </summary>
	/// <param name="args">The key event arguments.</param>
	/// <param name="shouldEvaluate">Set to true if the input should be processed for access keys.</param>
	internal void EvaluateAccessKeyMode(KeyRoutedEventArgs args, out bool shouldEvaluate)
	{
		shouldEvaluate = false;
		_forceQuit = false;

		var key = args.Key;
		var isKeyDown = !args.KeyStatus.IsKeyReleased;
		var isKeyUp = args.KeyStatus.IsKeyReleased;
		var isMenuKeyDown = args.KeyStatus.IsMenuKeyDown;
		var modifiers = args.KeyboardModifiers;
		var isAltPressed = (modifiers & VirtualKeyModifiers.Menu) == VirtualKeyModifiers.Menu;
		var isCtrlPressed = (modifiers & VirtualKeyModifiers.Control) == VirtualKeyModifiers.Control;
		var isAltKey = key == VirtualKey.Menu;
		var isNumpadInput = key >= VirtualKey.NumberPad0 && key <= VirtualKey.NumberPad9;
		var isFunctionKey = key >= VirtualKey.F1 && key <= VirtualKey.F12;

		UpdateAKModeStateChangeLockout(key, isKeyDown, isAltKey, modifiers);

		// Skip if Ctrl is pressed, unicode keypad input, or function keys
		if (isCtrlPressed || IsUnicodeKeypadInput(isAltPressed, isNumpadInput) || isFunctionKey)
		{
			return;
		}

		if (InputShouldCauseAKModeExit(key))
		{
			if (_isActive)
			{
				_akModeChanged = true;
				_forceQuit = true;
				shouldEvaluate = true; // We need to fire Hide events on the currently shown elements
				SetIsActive(false);
			}
		}
		else
		{
			EvaluateAccessKeyModeImpl(isAltKey, isKeyDown, isKeyUp, key, isMenuKeyDown, isNumpadInput, out shouldEvaluate);
		}
	}

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

		var isAltAKMessage = IsAltAccessKeyMessage(isKeyDown, keyCode, isMenuKey);
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
					if ((_isActive == false && _canEnterAccessKeyMode) || (_isActive && !_lockExitingAccessKeyModeOnAltUp))
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
	/// This type of input is an access key key pressed down with alt. Should treat this as a valid accesskey and invoke.
	/// </summary>
	private bool IsAltAccessKeyMessage(bool isKeyDown, VirtualKey keyCode, bool isMenuKey)
	{
		// When Alt then A is pressed, the first KeyDown message will contain isMenuKey==true with subsequent ones having this field set to false.
		// Therefore, this blocks repeatedly navigating down an access key hierarchy without using alt +AK without lifting the key and repressing it
		return _isActive && isMenuKey && keyCode >= VirtualKey.Number0 && keyCode <= VirtualKey.Z && isKeyDown;
	}

	private bool IsValidAccessKeyMessage(
		bool isAltKey,
		bool isKeyDown,
		bool isKeyUp,
		VirtualKey keyCode,
		bool isMenuKey,
		bool isNumpadInput)
	{
		// All numeric access key messages are valid for both number and numpad keys when in Access Key mode.
		// When in hot-key mode, access keys are enabled only for number keys, but not numpad keys.
		// This follows the precedent set by Office's access keys, and helps disambiguate alt-numeric special
		// characters from access keys.

		// Note: In WinUI, XCP_CHAR is checked separately. In our port, we handle character input
		// through a separate path, so we check for printable key ranges here.
		var isCharacterKey = _isActive && isKeyDown && IsPrintableKey(keyCode);

		return isCharacterKey ||
			   (isAltKey && isKeyUp) ||
			   (keyCode == VirtualKey.Escape && isKeyDown && _isActive) ||
			   (!isAltKey && !_isActive && isMenuKey && !isNumpadInput);
	}

	private static bool IsPrintableKey(VirtualKey key)
	{
		// Letters A-Z, Numbers 0-9
		return (key >= VirtualKey.A && key <= VirtualKey.Z) ||
			   (key >= VirtualKey.Number0 && key <= VirtualKey.Number9) ||
			   (key >= VirtualKey.NumberPad0 && key <= VirtualKey.NumberPad9);
	}

	private static bool InputShouldCauseAKModeExit(VirtualKey key)
	{
		return key == VirtualKey.Up ||
			   key == VirtualKey.Down ||
			   key == VirtualKey.Left ||
			   key == VirtualKey.Right ||
			   key == VirtualKey.Tab ||
			   key == VirtualKey.Space ||
			   key == VirtualKey.Enter;
	}

	private bool IsUnicodeKeypadInput(bool isAltPressed, bool isNumpadInput)
	{
		return _isActive && isAltPressed && isNumpadInput;
	}

	private void UpdateAKModeStateChangeLockout(VirtualKey key, bool isKeyDown, bool isAltKey, VirtualKeyModifiers modifiers)
	{
		var isNakedAltKeyDown = isKeyDown && isAltKey && modifiers == VirtualKeyModifiers.Menu;
		var isNakedAltKeyUp = !isKeyDown && isAltKey && modifiers == VirtualKeyModifiers.None;

		if (isNakedAltKeyDown && _lockEnteringAccessKeyModeUntilAltUp == false)
		{
			_canEnterAccessKeyMode = true; // Can only enter AK Mode when this is set to true.
			_lockExitingAccessKeyModeOnAltUp = false;
		}
		else if (!isNakedAltKeyUp || _lockEnteringAccessKeyModeUntilAltUp)
		{
			// Any input after the alt down that is not an Alt up will cause can enter AccessKeyMode to toggle to false. This will prevent entering AK mode
			// When using control+alt+delete, Alt+f4 etc. Even if an alt key-up is received, without the corresponding key down no state change will occur.
			_canEnterAccessKeyMode = false;

			// if Alt was held down on the release of a key, toggle the latch so we do not enter AKmode on the alt release.
			if ((modifiers & VirtualKeyModifiers.Menu) == VirtualKeyModifiers.Menu)
			{
				_lockEnteringAccessKeyModeUntilAltUp = true;
			}
			else if (modifiers == VirtualKeyModifiers.None)
			{
				// All modifiers released. Reset the lock (This also handles the case for alt up)
				_lockEnteringAccessKeyModeUntilAltUp = false;
				// In the case Alt+Ak is used to invoke an access key, we do not want the end of that message (Alt up) to potentially cause
				// an AK mode exit. Note: this does not preclude the invoke causing an exit, only the Alt Up.
				_lockExitingAccessKeyModeOnAltUp = true;
			}
		}
	}

	private void OnIsActiveChanged()
	{
		IsActiveChanged?.Invoke(this, EventArgs.Empty);
	}
}
