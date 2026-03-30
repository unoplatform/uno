// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\InputInterceptor.h, tag winui3/release/1.4.3, commit 685d2bf

#if __SKIA__
#nullable enable

using Windows.System;

namespace Microsoft.UI.Xaml.Input;

/// <summary>
/// Input router for AccessKeys. Routes keyboard and pointer input to the AccessKey engine.
/// </summary>
internal sealed class AKInputInterceptor
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
	/// This is the entry way into access keys. This method takes the input message and funnels the necessary
	/// information that the AK system needs to build and invoke the scope.
	/// Returns true if the key was processed and should be marked as handled.
	/// </summary>
	internal bool TryProcessInputForAccessKey(in AccessKeyInputMessage message)
	{
		bool keyProcessed = false;

		TryProcessKeyImpl(in message, ref keyProcessed);

		return keyProcessed;
	}

	/// <summary>
	/// Process a character received event when already in AK mode.
	/// </summary>
	internal bool TryProcessInputForCharacterReceived(char character)
	{
		if (!_modeContainer.GetIsActive())
		{
			return false;
		}

		// We handle Escape key on Keydown, not CharacterReceived
		if (character == (char)VirtualKey.Escape)
		{
			return false;
		}

		var message = AccessKeyInputMessage.FromCharacter(character);
		bool keyProcessed = false;
		TryProcessKeyImpl(in message, ref keyProcessed);
		return keyProcessed;
	}

	/// <summary>
	/// Process pointer input to dismiss AK mode.
	/// </summary>
	internal void ProcessPointerInput()
	{
		if (_modeContainer.GetIsActive())
		{
			_scopeTree.ExitScope(true);
			_modeContainer.SetIsActive(false);
		}
	}

	private void TryProcessKeyImpl(in AccessKeyInputMessage message, ref bool keyProcessed)
	{
		keyProcessed = false;
		bool shouldEvaluate = false;

		// If we are attempting to enter AK mode, we need to scan the visual tree to verify that we have
		// access keys set anywhere in the entire xaml visual tree (all visual roots included)
		if (!_modeContainer.GetIsActive() &&
			(message.IsAltKey || message.HasMenuKeyDown))
		{
			bool shouldActivate = _treeAnalyzer.DoesTreeContainAKElement();

			if (!shouldActivate)
			{
				return;
			}
		}

		// We ask the ModeContainer to reevaluate what mode we should be on based on whether alt was
		// pressed during a keydown and what the character code is.
		_modeContainer.EvaluateAccessKeyMode(in message, out shouldEvaluate);

		// We only want to process this character code if we are in AK mode
		if (shouldEvaluate)
		{
			// Send the character code to the scope tree in order to start building the scopes.
			_scopeTree.ProcessCharacter((char)message.VirtualKey, out keyProcessed);
		}

		keyProcessed = ShouldMarkHandled(keyProcessed, in message);
	}

	private bool ShouldMarkHandled(bool handled, in AccessKeyInputMessage message)
	{
		// MUX: pMessage->m_msgID != XCP_KEYUP
		// Key up events are never marked as handled by the AK system.
		return !IsInExcludeList(in message) &&
			(handled || _modeContainer.GetIsActive()) && !message.IsKeyUp;
	}

	private bool IsInExcludeList(in AccessKeyInputMessage message)
	{
		// If we want to force an exit from AK mode, then it means we received an input that should flow through ak
		// and be processed. This is captured through the ShouldForciblyExitAKMode. If this value is true, it means
		// that modecontainer contains an element it feels should be part of this exclusion list.
		return _modeContainer.ShouldForciblyExitAKMode();
	}
}
#endif
