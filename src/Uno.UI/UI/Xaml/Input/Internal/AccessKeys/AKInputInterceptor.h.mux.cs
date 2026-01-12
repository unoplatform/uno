// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\InputInterceptor.h, tag winui3/release/1.5.3

#nullable enable

using System;
using Windows.System;
using Microsoft.UI.Xaml.Input;
using AKCommon = Microsoft.UI.Xaml.Input.AccessKeys;

namespace Uno.UI.Xaml.Input.AccessKeys;

/// <summary>
/// Entry point for access key input processing.
/// Intercepts keyboard input and routes it to the access key system.
/// </summary>
internal class AKInputInterceptor
{
	private readonly AKModeContainer _modeContainer;
	private readonly AKScopeTree _scopeTree;
	private readonly AKTreeAnalyzer _treeAnalyzer;

	internal AKInputInterceptor(AKModeContainer modeContainer, AKScopeTree scopeTree, AKTreeAnalyzer treeAnalyzer)
	{
		_modeContainer = modeContainer;
		_scopeTree = scopeTree;
		_treeAnalyzer = treeAnalyzer;
	}

	/// <summary>
	/// This is the entry way into access keys. This method takes the InputMessage and funnels the necessary information
	/// that the AK system needs to build and invoke the scope.
	/// We then figure out whether we can enter AK mode. If we are successful, then we have handled the message and entered AK mode,
	/// meaning that the system should not continue to process this message. If we were not successful, then return false and continue
	/// processing this message.
	/// </summary>
	/// <param name="args">The key event arguments.</param>
	/// <param name="keyProcessed">Set to true if the key was processed by the access key system.</param>
	internal void TryProcessInputForAccessKey(KeyRoutedEventArgs args, out bool keyProcessed)
	{
		keyProcessed = false;

		TryProcessKeyImpl(args, out keyProcessed);
	}

	/// <summary>
	/// Processes character received events for access key input.
	/// </summary>
	/// <param name="args">The character received event arguments.</param>
	/// <param name="keyProcessed">Set to true if the character was processed by the access key system.</param>
	internal void TryProcessInputForCharacterReceived(CharacterReceivedRoutedEventArgs args, out bool keyProcessed)
	{
		keyProcessed = false;

		if (_modeContainer.GetIsActive())
		{
			var keyCode = args.Character;
			// We handle Escape key on Keydown, not CharacterReceived
			if (keyCode != AKCommon.ESC)
			{
				_scopeTree.ProcessCharacter((char)keyCode, out keyProcessed);

				keyProcessed = ShouldMarkHandledForChar(keyProcessed, keyCode);
			}
		}
	}

	/// <summary>
	/// Processes pointer input to exit access key mode when user clicks.
	/// </summary>
	internal void ProcessPointerInput()
	{
		var isActive = _modeContainer.GetIsActive();
		if (isActive)
		{
			_scopeTree.ExitScope(isActive);
			_modeContainer.SetIsActive(false);
		}
	}

	private void TryProcessKeyImpl(KeyRoutedEventArgs args, out bool keyProcessed)
	{
		// AK_TRACE(L"AK> TryProcessKeyImpl %x\n", inputMessage->m_platformKeyCode);

		keyProcessed = false;
		bool shouldEvaluate = false;

		var key = args.Key;
		var isAltKey = key == VirtualKey.Menu;
		var isMenuKeyDown = args.KeyStatus.IsMenuKeyDown;

		// If we are attempting to entering AK mode, we need to scan the visual tree to verify that we have
		// access key set anywhere in the entire xaml visual tree (all visual roots included)
		if (!_modeContainer.GetIsActive() && (isAltKey || isMenuKeyDown))
		{
			var shouldActivate = _treeAnalyzer.DoesTreeContainAKElement();

			if (!shouldActivate)
			{
				// AK_TRACE(L"AK> TryProcessKeyImpl: AccessKey mode not activated because there are no AccessKeys in root scope.\n");
				return;
			}
		}

		// We ask the ModeContainer to reevaluate what mode we should be on based on whether alt was
		// pressed during a keydown and what the charactercode (unsigned int) is.
		_modeContainer.EvaluateAccessKeyMode(args, out shouldEvaluate);

		// We only want to process this character code if we are in AK mode
		if (shouldEvaluate)
		{
			// Convert virtual key to character for access key matching
			char character = GetCharacterFromKey(args);

			// Send the character code to the scope tree in order to start building the scopes.
			_scopeTree.ProcessCharacter(character, out keyProcessed);
		}

		keyProcessed = ShouldMarkHandled(keyProcessed, args);
	}

	private char GetCharacterFromKey(KeyRoutedEventArgs args)
	{
		var key = args.Key;

		// For Alt key, return the ALT constant
		if (key == VirtualKey.Menu)
		{
			return AKCommon.ALT;
		}

		// For Escape key, return the ESC constant
		if (key == VirtualKey.Escape)
		{
			return AKCommon.ESC;
		}

		// For character keys, use UnicodeKey if available
		if (args.UnicodeKey is char unicode)
		{
			return char.ToUpperInvariant(unicode);
		}

		// For letter keys A-Z
		if (key >= VirtualKey.A && key <= VirtualKey.Z)
		{
			return (char)key;
		}

		// For number keys 0-9
		if (key >= VirtualKey.Number0 && key <= VirtualKey.Number9)
		{
			return (char)key;
		}

		// For numpad keys
		if (key >= VirtualKey.NumberPad0 && key <= VirtualKey.NumberPad9)
		{
			return (char)('0' + (key - VirtualKey.NumberPad0));
		}

		return (char)0;
	}

	private bool ShouldMarkHandled(bool handled, KeyRoutedEventArgs args)
	{
		return !IsInExcludeList() &&
			(handled || _modeContainer.GetIsActive()) &&
			args.Key != VirtualKey.Escape;
	}

	private bool ShouldMarkHandledForChar(bool handled, char character)
	{
		return !IsInExcludeList() &&
			(handled || _modeContainer.GetIsActive()) &&
			character != AKCommon.ESC;
	}

	private bool IsInExcludeList()
	{
		// If we want to force an exit from AK mode, then it means we received an input that should flow through ak and
		// be processed. This is captured through the ShouldForciblyExitAKMode. If this value is true, it means that modecontainer
		// contains an element it feels should be part of this exclusion list
		return _modeContainer.ShouldForciblyExitAKMode();
	}
}
