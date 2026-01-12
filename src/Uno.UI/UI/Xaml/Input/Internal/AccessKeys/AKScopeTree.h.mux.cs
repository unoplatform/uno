// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\ScopeTree.h, tag winui3/release/1.5.3

#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Xaml.Core;
using AKCommon = Microsoft.UI.Xaml.Input.AccessKeys;

namespace Uno.UI.Xaml.Input.AccessKeys;

/// <summary>
/// Manages the access key scope hierarchy and processes input for access key navigation.
/// </summary>
internal class AKScopeTree
{
	private readonly AKScopeBuilder _scopeBuilder;
	private readonly AKTreeAnalyzer _treeAnalyzer;
	private readonly AKModeContainer _modeContainer;
	private AKScope? _current;
	private FocusManager? _focusManager;

	internal AKScopeTree(AKScopeBuilder builder, AKTreeAnalyzer treeAnalyzer, AKModeContainer modeContainer)
	{
		_scopeBuilder = builder;
		_treeAnalyzer = treeAnalyzer;
		_modeContainer = modeContainer;
	}

	/// <summary>
	/// Processes a character input for access key navigation.
	/// </summary>
	/// <param name="character">The character typed by the user.</param>
	/// <param name="wasInvoked">Set to true if an access key was invoked or partially matched.</param>
	internal void ProcessCharacter(char character, out bool wasInvoked)
	{
		wasInvoked = false;

		var wasActive = _modeContainer.GetIsActive();

		// First, check to see if we're exiting with an alt key. In this case Mnemonics mode is not active (GetIsActive == false on entering this method) unlike the case
		// Where ProcessCharacter is called with a mode entering alt (GetIsActive == true in that case)
		// We can also exit ak mode based on certain input. ShouldForciblyExitAKMode in mode container captures this detail, therefore, we should also
		// exit when this is true
		// If we are not, and if we are just entering AK mode, then we want to create the new scope but not invoke it. This is because the scope being
		// created is the root scope that contains all the scopes that the user can interact with
		if ((!wasActive && character == AKCommon.ALT) || _modeContainer.ShouldForciblyExitAKMode())
		{
			ExitScope(true);
		}
		else if (_modeContainer.HasAKModeChanged())
		{
			// If this is a hotkey or the first 'normal' invocation build an access key scope
			UpdateScopeImpl(wasActive, GetFocusedElementNoRef());
		}

		if (character == AKCommon.ESC)
		{
			ProcessEscapeKey(out wasInvoked);
		}
		else if (character != AKCommon.ALT)
		{
			ProcessNormalKey(character, wasActive, out wasInvoked);
		}

		var isActive = _modeContainer.GetIsActive();
		var wasDeactivated = wasActive && !isActive;
		// The way hotkeys flows through this code is that we do not set mnemonics mode active (e.g. GetIsActive=false) but we set HasAKModeChanged to true.
		// Todo: Refactor this obscure state into an enum so it's a little more clear what exactly is being handled (e.g. normal AK, escape, hotkey etc).
		var wasHotkeyInvocation = !isActive && _modeContainer.HasAKModeChanged(); // If this hotkey invocation did not enter a scope then exit the scope.
		if (wasDeactivated || wasHotkeyInvocation)
		{
			ExitScope(wasActive);
		}
	}

	/// <summary>
	/// Called by FocusManager to update the current scope.
	/// </summary>
	internal void UpdateScope()
	{
		var isActive = _modeContainer.GetIsActive();
		if (!isActive)
		{
			return;
		}

		UpdateScopeImpl(isActive, GetFocusedElementNoRef());
	}

	/// <summary>
	/// Enters access key mode with the current scope.
	/// </summary>
	internal void EnterScope()
	{
		UpdateScopeImpl(true, GetFocusedElementNoRef());
	}

	/// <summary>
	/// Exits the current scope and hides access keys if active.
	/// </summary>
	/// <param name="isActive">Whether access key mode was active.</param>
	internal void ExitScope(bool isActive)
	{
		// AK_TRACE(L"AK> ExitScope\n");
		if (_current is not null)
		{
			if (isActive)
			{
				_current.HideAccessKeys();
			}
			_current = null;
		}
	}

	/// <summary>
	/// Sets the focus manager for access key navigation.
	/// </summary>
	internal void SetFocusManager(FocusManager? focusManager)
	{
		_focusManager = focusManager;
	}

	/// <summary>
	/// Adds an element to the current scope if access key mode is active.
	/// </summary>
	internal void AddElement(DependencyObject element)
	{
		// ASSERT(m_modeContainer.GetIsActive());

		var currentScope = _current;

		if (currentScope is not null && _treeAnalyzer.IsValidAKElement(element))
		{
			var owner = _treeAnalyzer.GetScopeOwner(element);
			var scopeParent = currentScope.GetScopeParent();

			if (currentScope.ShouldElementEnteringTreeUpdateScope(owner))
			{
				// For us to have reached this code path means we have to be in AK mode, so it
				// is safe for us to pass in true
				UpdateScopeImpl(true, element);
			}
			else if (ReferenceEquals(owner, scopeParent))
			{
				currentScope.AddToAccessKeyOwner(element);
			}
		}
	}

	/// <summary>
	/// Removes an element from the current scope if access key mode is active.
	/// </summary>
	internal void RemoveElement(DependencyObject element)
	{
		// ASSERT(m_modeContainer.GetIsActive());

		var currentScope = _current;

		if (currentScope is not null && _treeAnalyzer.IsAccessKey(element))
		{
			var owner = _treeAnalyzer.GetScopeOwner(element);
			var scopeParent = currentScope.GetScopeParent();

			if (ReferenceEquals(owner, scopeParent))
			{
				currentScope.RemoveFromAccessKeyOwner(element);
			}
			// There could be the situation where the scope owner is being removed. In that case, we
			// should update the entire scope
			else if (ReferenceEquals(element, scopeParent) && scopeParent is not null && _treeAnalyzer.IsValidAKElement(scopeParent))
			{
				UpdateScopeImpl(true, scopeParent);
			}
		}
	}

	/// <summary>
	/// Called when an element's IsEnabled property changes.
	/// </summary>
	internal void OnIsEnabledChanged(DependencyObject element, bool isEnabled)
	{
		var currentScope = _current;

		if (currentScope is not null && _treeAnalyzer.IsAccessKey(element))
		{
			if (isEnabled)
			{
				AddElement(element);
			}
			else
			{
				RemoveElement(element);
			}
		}
	}

	/// <summary>
	/// Called when an element's Visibility property changes.
	/// </summary>
	internal void OnVisibilityChanged(DependencyObject element, Visibility visibility)
	{
		var currentScope = _current;

		if (currentScope is not null && _treeAnalyzer.IsAccessKey(element))
		{
			if (visibility == Visibility.Visible)
			{
				AddElement(element);
			}
			else if (visibility == Visibility.Collapsed)
			{
				RemoveElement(element);
			}
		}
	}

	private void UpdateScopeImpl(bool isActive, DependencyObject? scopeElement)
	{
		DependencyObject? newOwner = null;

		// If this is a hotkey invocation then isActive == false.
		// This will cause the root scope to be entered in the call to EnterScope at the end of the method
		if (isActive)
		{
			newOwner = scopeElement is not null ? _treeAnalyzer.GetScopeOwner(scopeElement) : null;
			if (_current is not null)
			{
				var oldOwner = _current.GetScopeParent();
				if (ReferenceEquals(newOwner, oldOwner))
				{
					return;
				}
			}
		}

		EnterScope(newOwner, isActive);
	}

	private DependencyObject? GetFocusedElementNoRef()
	{
		if (_focusManager is not null)
		{
			return _focusManager.FocusedElement;
		}
		return null;
	}

	private void ProcessEscapeKey(out bool wasInvoked)
	{
		var current = _current;
		wasInvoked = false;

		if (current is not null)
		{
			// If we are filtering scope owners, back off one letter.
			// If we are in the root scope (GetScope on the parent) returns nullptr, then exit mnemonics mode.
			// If a scope has no defined parent (this is set at construction), or if the parent is part of the root scope leave mnemonics mode.
			// Otherwise, we will attempt to 'pop' the scope by entering the scope of the parent element
			if (current.IsScopeFilteringInput)
			{
				current.ProcessEscapeKey();
			}
			else
			{
				BackOutToNextValidParentScope(current);
			}
		}
	}

	/// <summary>
	/// Walk up the scope parents to find the closest valid scope. Enter that scope.
	/// Exit AccessKey DisplayMode if no valid ancestor scope is found. The caller is responsible for
	/// calling ExitScope in that case (ProcessCharacter will do this).
	/// </summary>
	private void BackOutToNextValidParentScope(AKScope initialScope)
	{
		// In the past, we only called UpdateScopeImpl here when IsValidAKElement() returned true from
		// GetScopeParent(). But this resulted in some situations where the user gets stuck in a scope and can't
		// back out. Instead, we walk up the scope parent tree until we find a valid parent scope we can back
		// up into.
		int triesLeft = 100;
		var scopeParent = initialScope.GetScopeParent();
		while (scopeParent is not null)
		{
			UpdateScopeImpl(_modeContainer.GetIsActive(), scopeParent);
			var didScopeChange = !ReferenceEquals(initialScope, _current);
			if (didScopeChange)
			{
				// We successfully entered a new valid scope. All done.
				return;
			}

			// The scope will be unchanged here if the AK scope that contains "scopeParent" doesn't have any AccessKeys.
			// If the scope is unchanged, back it out again.
			scopeParent = _treeAnalyzer.GetScopeOwner(scopeParent);

			// If we hit this failfast, it means the scope tree is 100 levels deep. It's more likely we hit some
			// kind of cycle in the logic to walk the scope tree, so we just failfast.
			if (triesLeft-- == 0)
			{
				throw new InvalidOperationException("Access key scope tree appears to have a cycle.");
			}
		}

		// We walked all the way up without finding any valid scopes. Exit AccessKey DisplayMode.
		_modeContainer.SetIsActive(false);
	}

	private AKScope? ConstructScope(DependencyObject? element)
	{
		// TraceAccessKeyScopeBuilderConstructScopeBegin();
		var newScope = _scopeBuilder.ConstructScope(element);
		// TraceAccessKeyScopeBuilderConstructScopeEnd();
		return newScope;
	}

	private void ProcessNormalKey(char character, bool wasActive, out bool wasInvoked)
	{
		wasInvoked = false;

		// Invoke can be reentrant, we need to protect _current by having our own reference
		var current = _current;
		if (current is not null)
		{
			// If an AccessKeyOwner was found and Invoked or if there was partial matching, wasInvoked <- true
			//
			// In the case where ProcessNormalKey is called in HotKey mode, wasActive will be false. Passing this into the scope will suppress
			// the partial matching feature.
			current.Invoke(character, wasActive /* allow partial match filtering in the scope */, out var invokeResult);

			wasInvoked = invokeResult.InvokeAttempted;

			DependencyObject? invokedElement = null;
			invokeResult.InvokedElement?.TryGetTarget(out invokedElement);

			// If the AKO invoked is a scope owner, Call update scope to change scope to that one
			// If the invoked element is nullptr, don't change to this scope because it's root scope
			// Allowing a navigation into root scope would allow for scope cycles to form.
			if (invokedElement is not null)
			{
				// We successfully found an element to be invoked, but it failed to find a valid pattern. As a result, we will give focus to the element
				if (!invokeResult.InvokeFoundValidPattern)
				{
					// TODO UNO: Implement focus movement for invalid pattern
					// if (FocusProperties::IsFocusable(invokedElement.get(), false /*ignoreOffScreenPosition*/))
					// {
					//     const Focus::FocusMovementResult result = m_pFocusManager->SetFocusedElement(
					//         Focus::FocusMovement(
					//             invokedElement,
					//             DirectUI::FocusNavigationDirection::None,
					//             DirectUI::FocusState::Keyboard));
					//     IFC_RETURN(result.GetHResult());
					// }
					if (invokedElement is Microsoft.UI.Xaml.Controls.Control control && control.IsEnabled)
					{
						control.Focus(FocusState.Keyboard);
					}
				}

				if (_treeAnalyzer.IsScopeOwner(invokedElement))
				{
					// This is the case that a hotkey invokes a scope owner - we need to set AK mode active to prevent the scope from going stale.
					// The invoke handles entering the scope.
					if (!wasActive)
					{
						_modeContainer.SetIsActive(true);
					}
					EnterScope(invokedElement, wasActive);
				}
				// If the AKO invoked has DismissAccessKeyOnInvoke set to true, exit AK mode now
				// Intentionally not allowing a navigation to also dismiss AK mode.
				else if (AKCommon.DismissOnInvoked(invokedElement))
				{
					_modeContainer.SetIsActive(false); // Note this will propagate responsibility of exiting the scope to ScopeTree::ProcessCharacter
				}
				else if (wasActive) // If this was not a hotkey invoke, e.g. wasActive == false...
				{
					_current?.ShowAccessKeys(); // If not dismissing on invoke, then let's refresh the visuals if AKMode was active.
				}
			}
		}
	}

	private void EnterScope(DependencyObject? element, bool isActive)
	{
		// AK_TRACE(L"AK> EnterScope %p\n", element);

		var newScope = ConstructScope(element);

		// We only want to change the current scope if the creation of the new scope was valid
		if (newScope is not null)
		{
			ExitScope(isActive);
			_current = newScope;
			if (isActive)
			{
				_current.ShowAccessKeys();
			}
		}
	}

	private bool IsRootScope()
	{
		var scopeParent = _current?.GetScopeParent();

		// If the seed element is nullptr, then this is a root scope and we should exit
		return scopeParent is null;
	}
}
