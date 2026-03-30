// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\ScopeTree.h, tag winui3/release/1.4.3, commit 685d2bf

#if __SKIA__
#nullable enable

using System;
using Uno.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Input;

/// <summary>
/// Central coordinator for AccessKey scope navigation. Manages the current scope,
/// processes input characters, and navigates between parent/child scopes.
/// </summary>
internal sealed class AKScopeTree
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

	internal void SetFocusManager(FocusManager? focusManager)
	{
		_focusManager = focusManager;
	}

	internal void ProcessCharacter(char character, out bool wasInvoked)
	{
		wasInvoked = false;

		bool wasActive = _modeContainer.GetIsActive();

		// First, check to see if we're exiting with an alt key. In this case access keys mode is not active
		// (GetIsActive == false on entering this method) unlike the case where ProcessCharacter is called with
		// a mode entering alt (GetIsActive == true in that case).
		// We can also exit ak mode based on certain input. ShouldForciblyExitAKMode in mode container captures this detail.
		if ((!wasActive && character == AccessKeys.ALT) || _modeContainer.ShouldForciblyExitAKMode())
		{
			ExitScope(true);
		}
		else if (_modeContainer.HasAKModeChanged())
		{
			// If this is a hotkey or the first 'normal' invocation build an access key scope
			UpdateScopeImpl(wasActive, GetFocusedElementNoRef());
		}

		if (character == AccessKeys.ESC)
		{
			ProcessEscapeKey(out wasInvoked);
		}
		else if (character != AccessKeys.ALT)
		{
			ProcessNormalKey(character, wasActive, out wasInvoked);
		}

		bool isActive = _modeContainer.GetIsActive();
		bool wasDeactivated = wasActive && !isActive;
		// The way hotkeys flow through this code is that we do not set access keys mode active (e.g. GetIsActive=false)
		// but we set HasAKModeChanged to true.
		bool wasHotkeyInvocation = !isActive && _modeContainer.HasAKModeChanged();
		if (wasDeactivated || wasHotkeyInvocation)
		{
			ExitScope(wasActive);
		}
	}

	/// <summary>
	/// Called by FocusManager when focus changes during AK mode.
	/// </summary>
	internal void UpdateScope()
	{
		if (!_modeContainer.GetIsActive())
		{
			return;
		}

		UpdateScopeImpl(_modeContainer.GetIsActive(), GetFocusedElementNoRef());
	}

	internal void EnterScope()
	{
		UpdateScopeImpl(true, GetFocusedElementNoRef());
	}

	internal void ExitScope(bool isActive)
	{
		if (_current is not null)
		{
			if (isActive)
			{
				_current.HideAccessKeys();
			}

			_current = null;
		}
	}

	internal void AddElement(DependencyObject element)
	{
		var currentScope = _current;

		if (currentScope is not null && _treeAnalyzer.IsValidAKElement(element))
		{
			var owner = _treeAnalyzer.GetScopeOwner(element);
			var scopeParent = currentScope.GetScopeParent();

			if (currentScope.ShouldElementEnteringTreeUpdateScope(owner))
			{
				// For us to have reached this code path means we have to be in AK mode, so it
				// is safe for us to pass in true.
				UpdateScopeImpl(true, element);
			}
			else if (ReferenceEquals(owner, scopeParent))
			{
				currentScope.AddToAccessKeyOwner(element);
			}
		}
	}

	internal void RemoveElement(DependencyObject element)
	{
		var currentScope = _current;

		if (currentScope is not null && _treeAnalyzer.IsAccessKey(element))
		{
			var owner = _treeAnalyzer.GetScopeOwner(element);
			var scopeParent = currentScope.GetScopeParent();

			if (ReferenceEquals(owner, scopeParent))
			{
				currentScope.RemoveFromAccessKeyOwner(element);
			}
			// There could be the situation where the scope owner is being removed. In that case,
			// we should update the entire scope.
			else if (ReferenceEquals(element, scopeParent) && scopeParent is not null && _treeAnalyzer.IsValidAKElement(scopeParent))
			{
				UpdateScopeImpl(true, scopeParent);
			}
		}
	}

	private void UpdateScopeImpl(bool isActive, DependencyObject? scopeElement)
	{
		DependencyObject? pNewOwner = null;

		// If this is a hotkey invocation then isActive == false.
		// This will cause the root scope to be entered in the call to EnterScope at the end of the method.
		if (isActive)
		{
			pNewOwner = scopeElement is not null ? _treeAnalyzer.GetScopeOwner(scopeElement) : null;
			if (_current is not null)
			{
				var pOldOwner = _current.GetScopeParent();
				if (ReferenceEquals(pNewOwner, pOldOwner))
				{
					return;
				}
			}
		}

		EnterScope(pNewOwner, isActive);
	}

	private DependencyObject? GetFocusedElementNoRef()
	{
		return _focusManager?.FocusedElement as DependencyObject;
	}

	private void ProcessEscapeKey(out bool wasInvoked)
	{
		var current = _current;
		wasInvoked = false;

		if (current is not null)
		{
			// If we are filtering scope owners, back off one letter.
			// If we are in the root scope, then exit access keys mode.
			// If a scope has no defined parent, or if the parent is part of the root scope, leave access keys mode.
			// Otherwise, we will attempt to 'pop' the scope by entering the scope of the parent element.
			if (current.IsScopeFilteringInput())
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
	/// Exit AccessKey DisplayMode if no valid ancestor scope is found.
	/// </summary>
	private void BackOutToNextValidParentScope(AKScope initialScope)
	{
		int triesLeft = 100;
		var scopeParent = initialScope.GetScopeParent();
		while (scopeParent is not null)
		{
			UpdateScopeImpl(_modeContainer.GetIsActive(), scopeParent);
			bool didScopeChange = !ReferenceEquals(initialScope, _current);
			if (didScopeChange)
			{
				// We successfully entered a new valid scope. All done.
				return;
			}

			// The scope will be unchanged here if the AK scope that contains "scopeParent" doesn't have any AccessKeys.
			// If the scope is unchanged, back it out again.
			scopeParent = _treeAnalyzer.GetScopeOwner(scopeParent);

			if (triesLeft-- == 0)
			{
				// If we hit this, it means the scope tree is 100 levels deep.
				// It's more likely we hit some kind of cycle in the logic to walk the scope tree.
				break;
			}
		}

		// We walked all the way up without finding any valid scopes. Exit AccessKey DisplayMode.
		_modeContainer.SetIsActive(false);
	}

	private void ProcessNormalKey(char character, bool wasActive, out bool wasInvoked)
	{
		wasInvoked = false;

		// Invoke can be reentrant, we need to protect m_current by having our own reference
		var current = _current;
		if (current is not null)
		{
			// If an AccessKeyOwner was found and Invoked or if there was partial matching, wasInvoked <- true
			// In the case where ProcessNormalKey is called in HotKey mode, wasActive will be false.
			// Passing this into the scope will suppress the partial matching feature.
			current.Invoke(character, wasActive /* allow partial match filtering in the scope */, out var invokeResult);

			wasInvoked = invokeResult.InvokeAttempted;

			DependencyObject? invokedElement = null;
			invokeResult.InvokedElement?.TryGetTarget(out invokedElement);

			// If the AKO invoked is a scope owner, Call update scope to change scope to that one.
			// If the invoked element is null, don't change to this scope because it's root scope.
			// Allowing a navigation into root scope would allow for scope cycles to form.
			if (invokedElement is not null)
			{
				// We successfully found an element to be invoked, but it failed to find a valid pattern.
				// As a result, we will give focus to the element.
				if (!invokeResult.InvokeFoundValidPattern)
				{
					if (FocusProperties.IsFocusable(invokedElement))
					{
						_focusManager?.SetFocusedElement(
							new FocusMovement(
								invokedElement,
								FocusNavigationDirection.None,
								FocusState.Keyboard));
					}
				}

				if (_treeAnalyzer.IsScopeOwner(invokedElement))
				{
					// This is the case that a hotkey invokes a scope owner - we need to set AK mode active
					// to prevent the scope from going stale. The invoke handles entering the scope.
					if (!wasActive)
					{
						_modeContainer.SetIsActive(true);
					}

					EnterScope(invokedElement, wasActive);
				}
				// If the AKO invoked has DismissAccessKeyOnInvoke set to true, exit AK mode now.
				// Intentionally not allowing a navigation to also dismiss AK mode.
				else if (AccessKeys.DismissOnInvoked(invokedElement))
				{
					_modeContainer.SetIsActive(false); // Note this will propagate responsibility of exiting the scope to ProcessCharacter
				}
				else if (wasActive) // If this was not a hotkey invoke...
				{
					_current?.ShowAccessKeys(); // If not dismissing on invoke, then let's refresh the visuals if AKMode was active.
				}
			}
		}
	}

	private void EnterScope(DependencyObject? element, bool isActive)
	{
		var newScope = _scopeBuilder.ConstructScope(element);

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
}
#endif
