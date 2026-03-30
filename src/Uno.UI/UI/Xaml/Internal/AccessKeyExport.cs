// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\AKExport.h, tag winui3/release/1.4.3, commit 685d2bf
// MUX Reference dxaml\xcp\components\AccessKeys\Export\AKExport.cpp, tag winui3/release/1.4.3, commit 685d2bf

#if __SKIA__
#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Uno.UI.Xaml.Core;

/// <summary>
/// Facade that coordinates all AccessKey engine components.
/// Replaces the C++ AccessKeyExport with its pimpl pattern.
/// </summary>
internal sealed class AccessKeyExport
{
	private readonly AKVisualTreeFinder _treeLibrary;
	private readonly AKModeContainer _modeContainer;
	private readonly AKTreeAnalyzer _treeAnalyzer;
	private readonly AKScopeBuilder _scopeBuilder;
	private readonly AKScopeTree _scopeTree;
	private readonly AKInputInterceptor _inputInterceptor;

	private bool _isVisualTreeValid;
	private bool _isFocusManagerValid;

	internal AccessKeyExport()
	{
		_treeLibrary = new AKVisualTreeFinder();
		_modeContainer = new AKModeContainer();
		_treeAnalyzer = new AKTreeAnalyzer(_treeLibrary);
		_scopeBuilder = new AKScopeBuilder(_treeAnalyzer);
		_scopeTree = new AKScopeTree(_scopeBuilder, _treeAnalyzer, _modeContainer);
		_inputInterceptor = new AKInputInterceptor(_modeContainer, _scopeTree, _treeAnalyzer);
	}

	private bool IsValid => _isFocusManagerValid && _isVisualTreeValid;

	internal bool IsActive => _modeContainer.GetIsActive();

	internal AKModeContainer ModeContainer => _modeContainer;

	/// <summary>
	/// Try to process a keyboard input for AccessKey navigation.
	/// Returns true if the key was handled by the AccessKey system.
	/// </summary>
	internal bool TryProcessInputForAccessKey(in AccessKeyInputMessage message)
	{
		if (!IsValid)
		{
			return false;
		}

		return _inputInterceptor.TryProcessInputForAccessKey(in message);
	}

	/// <summary>
	/// Process pointer input (dismiss AK mode on pointer interaction).
	/// </summary>
	internal void ProcessPointerInput()
	{
		if (!IsValid)
		{
			return;
		}

		_inputInterceptor.ProcessPointerInput();
	}

	/// <summary>
	/// Process a character received event when in AK mode.
	/// Returns true if the character was handled.
	/// </summary>
	internal bool TryProcessInputForCharacterReceived(char character)
	{
		if (!IsValid)
		{
			return false;
		}

		return _inputInterceptor.TryProcessInputForCharacterReceived(character);
	}

	/// <summary>
	/// Update the current scope (called by FocusManager on focus changes).
	/// </summary>
	internal void UpdateScope()
	{
		if (!IsValid)
		{
			return;
		}

		_scopeTree.UpdateScope();
	}

	/// <summary>
	/// Add an element to the active AK mode scope.
	/// </summary>
	internal void AddElementToAKMode(DependencyObject element)
	{
		if (!IsValid)
		{
			return;
		}

		_scopeTree.AddElement(element);
	}

	/// <summary>
	/// Remove an element from the active AK mode scope.
	/// </summary>
	internal void RemoveElementFromAKMode(DependencyObject element)
	{
		if (!IsValid)
		{
			return;
		}

		_scopeTree.RemoveElement(element);
	}

	/// <summary>
	/// Cleanup and exit the current scope.
	/// </summary>
	internal void CleanupAndExitCurrentScope()
	{
		if (!IsValid)
		{
			return;
		}

		_scopeTree.ExitScope(IsActive);
	}

	/// <summary>
	/// Exit AccessKey mode programmatically.
	/// </summary>
	internal void ExitAccessKeyMode()
	{
		CleanupAndExitCurrentScope();
		_modeContainer.SetIsActive(false);
	}

	/// <summary>
	/// Enter AccessKey mode programmatically.
	/// </summary>
	internal void EnterAccessKeyMode()
	{
		if (_modeContainer.GetIsActive())
		{
			return;
		}

		_modeContainer.SetIsActive(true);
		_scopeTree.EnterScope();
	}

	/// <summary>
	/// Set the visual tree for the AccessKey engine.
	/// </summary>
	internal void SetVisualTree(VisualTree? tree)
	{
		_treeLibrary.SetVisualTree(tree);
		_isVisualTreeValid = tree is not null;
	}

	/// <summary>
	/// Set the focus manager for the AccessKey engine.
	/// </summary>
	internal void SetFocusManager(FocusManager? focusManager)
	{
		_scopeTree.SetFocusManager(focusManager);
		_isFocusManagerValid = focusManager is not null;
	}

	/// <summary>
	/// Set the callback for when AK mode active state changes.
	/// </summary>
	internal void SetOnIsActiveChanged(Action? callback)
	{
		_modeContainer.OnIsActiveChanged = callback;
	}
}
#endif
