// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\AKExport.h and Export\AKExport.cpp, tag winui3/release/1.5.3

#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Xaml.Input.AccessKeys;

namespace Uno.UI.Xaml.Core;

/// <summary>
/// Exports access key functionality to the rest of the XAML framework.
/// Coordinates tree analysis, scope management, input handling, and mode state.
/// </summary>
internal class AccessKeyExport
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
		_isVisualTreeValid = false;
		_isFocusManagerValid = false;
	}

	private bool IsValid => _isFocusManagerValid && _isVisualTreeValid;

	/// <summary>
	/// Attempts to process a key event for access key navigation.
	/// </summary>
	/// <param name="args">The key event arguments.</param>
	/// <param name="keyProcessed">Set to true if the key was processed by the access key system.</param>
	internal void TryProcessInputForAccessKey(KeyRoutedEventArgs args, out bool keyProcessed)
	{
		keyProcessed = false;
		if (IsValid)
		{
			_inputInterceptor.TryProcessInputForAccessKey(args, out keyProcessed);
		}
	}

	/// <summary>
	/// Attempts to process a character received event for access key navigation.
	/// </summary>
	/// <param name="args">The character received event arguments.</param>
	/// <param name="keyProcessed">Set to true if the character was processed by the access key system.</param>
	internal void TryProcessInputForCharacterReceived(CharacterReceivedRoutedEventArgs args, out bool keyProcessed)
	{
		keyProcessed = false;
		if (IsValid)
		{
			_inputInterceptor.TryProcessInputForCharacterReceived(args, out keyProcessed);
		}
	}

	/// <summary>
	/// Called by FocusManager to update the current access key scope.
	/// </summary>
	internal void UpdateScope()
	{
		if (IsValid)
		{
			_scopeTree.UpdateScope();
		}
	}

	/// <summary>
	/// Processes pointer input to exit access key mode when the user clicks.
	/// </summary>
	internal void ProcessPointerInput()
	{
		if (IsValid)
		{
			_inputInterceptor.ProcessPointerInput();
		}
	}

	/// <summary>
	/// Adds an element to the current access key mode scope.
	/// Called when an element with an access key enters the visual tree.
	/// </summary>
	internal void AddElementToAKMode(DependencyObject element)
	{
		if (IsValid)
		{
			_scopeTree.AddElement(element);
		}
	}

	/// <summary>
	/// Removes an element from the current access key mode scope.
	/// Called when an element with an access key leaves the visual tree.
	/// </summary>
	internal void RemoveElementFromAKMode(DependencyObject element)
	{
		if (IsValid)
		{
			_scopeTree.RemoveElement(element);
		}
	}

	/// <summary>
	/// Called when an element's IsEnabled property changes.
	/// </summary>
	internal void OnIsEnabledChanged(DependencyObject element, bool isEnabled)
	{
		if (IsValid)
		{
			_scopeTree.OnIsEnabledChanged(element, isEnabled);
		}
	}

	/// <summary>
	/// Called when an element's Visibility property changes.
	/// </summary>
	internal void OnVisibilityChanged(DependencyObject element, Visibility visibility)
	{
		if (IsValid)
		{
			_scopeTree.OnVisibilityChanged(element, visibility);
		}
	}

	/// <summary>
	/// Gets whether access key mode is currently active.
	/// </summary>
	internal bool IsActive => _modeContainer.GetIsActive();

	/// <summary>
	/// Gets the mode container for direct access to mode state.
	/// </summary>
	internal AKModeContainer ModeContainer => _modeContainer;

	/// <summary>
	/// Sets the visual tree for access key navigation.
	/// </summary>
	internal void SetVisualTree(VisualTree? tree)
	{
		_treeLibrary.SetVisualTree(tree);
		_isVisualTreeValid = tree is not null;
	}

	/// <summary>
	/// Sets the focus manager for access key navigation.
	/// </summary>
	internal void SetFocusManager(FocusManager? focusManager)
	{
		_scopeTree.SetFocusManager(focusManager);
		_modeContainer.SetFocusManager(focusManager);
		_isFocusManagerValid = focusManager is not null;
	}

	/// <summary>
	/// Cleans up and exits the current access key scope.
	/// </summary>
	internal void CleanupAndExitCurrentScope()
	{
		if (IsValid)
		{
			_scopeTree.ExitScope(IsActive);
		}
	}

	/// <summary>
	/// Exits access key mode completely.
	/// </summary>
	internal void ExitAccessKeyMode()
	{
		CleanupAndExitCurrentScope();
		_modeContainer.SetIsActive(false);
	}

	/// <summary>
	/// Programmatically enters access key mode.
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
}
