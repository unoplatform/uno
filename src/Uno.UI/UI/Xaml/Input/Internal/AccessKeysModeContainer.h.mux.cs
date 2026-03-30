// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\ModeContainer.h, tag winui3/release/1.4.3, commit 685d2bf

#if __SKIA__
#nullable enable

using System;
using Windows.System;

namespace Microsoft.UI.Xaml.Input;

/// <summary>
/// Alt key state machine for AccessKey mode activation/deactivation.
/// </summary>
internal sealed partial class AKModeContainer
{
	private bool _isActive;
	private bool _akModeChanged;
	private bool _forceQuit;
	// Only want to enter AK mode when an alt key was pressed and released without other key input.
	// This bool toggles to false whenever input that should disallow AK mode is entered.
	private bool _canEnterAccessKeyMode;
	private bool _lockEnteringAccessKeyModeUntilAltUp;
	private bool _lockExitingAccessKeyModeOnAltUp;

	/// <summary>
	/// Callback invoked when IsActive changes. Set by AccessKeyExport.
	/// </summary>
	internal Action? OnIsActiveChanged { get; set; }

	internal bool GetIsActive() => _isActive;
	internal bool HasAKModeChanged() => _akModeChanged;
	internal bool ShouldForciblyExitAKMode() => _forceQuit;

	internal void SetIsActive(bool newValue)
	{
		if (_isActive != newValue)
		{
			_isActive = newValue;
			OnIsActiveChanged?.Invoke();
		}
	}

	/// <summary>
	/// Evaluate input message to see if we should activate access key navigation.
	/// Mode will change synchronously and fire IsActiveChanged event.
	/// </summary>
	internal void EvaluateAccessKeyMode(in AccessKeyInputMessage message, out bool shouldEvaluate)
	{
		shouldEvaluate = false;
		_forceQuit = false;

		UpdateAKModeStateChangeLockout(in message);

		if (!message.IsChar && ((message.Modifiers & VirtualKeyModifiers.Control) == VirtualKeyModifiers.Control ||
			IsUnicodeKeypadInput(in message) ||
			IsFunctionKey(in message)))
		{
			return;
		}

		if (InputShouldCauseAKModeExit(in message))
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
			EvaluateAccessKeyModeImpl(
				message.IsAltKey,
				message.IsKeyDown,
				message.IsKeyUp,
				message.VirtualKey,
				message.HasMenuKeyDown,
				IsNumpadInput(in message),
				out shouldEvaluate);
		}
	}

	private static bool InputShouldCauseAKModeExit(in AccessKeyInputMessage message)
	{
		return message.VirtualKey == VirtualKey.Up ||
			   message.VirtualKey == VirtualKey.Down ||
			   message.VirtualKey == VirtualKey.Left ||
			   message.VirtualKey == VirtualKey.Right ||
			   message.VirtualKey == VirtualKey.Tab ||
			   message.VirtualKey == VirtualKey.Space ||
			   message.VirtualKey == VirtualKey.Enter;
	}

	private bool IsUnicodeKeypadInput(in AccessKeyInputMessage message)
	{
		return _isActive &&
			(message.Modifiers & VirtualKeyModifiers.Menu) != 0 &&
			IsNumpadInput(in message);
	}

	private static bool IsNumpadInput(in AccessKeyInputMessage message)
	{
		return message.VirtualKey >= VirtualKey.NumberPad0 && message.VirtualKey <= VirtualKey.NumberPad9;
	}

	private static bool IsFunctionKey(in AccessKeyInputMessage message)
	{
		return message.VirtualKey >= VirtualKey.F1 && message.VirtualKey <= VirtualKey.F12;
	}
}
#endif
