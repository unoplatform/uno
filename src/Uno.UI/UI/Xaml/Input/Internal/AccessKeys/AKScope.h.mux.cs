// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\Scope.h, tag winui3/release/1.5.3

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Input.AccessKeys;

/// <summary>
/// Represents a set of Access Key owners of which any could be invoked.
/// Checks current input fed from the ScopeTree for matching keys and invokes on a match.
/// </summary>
internal class AKScope
{
	private string _inputAccumulator = "";
	private readonly Dictionary<AKAccessKey, AKOwner> _accessKeyOwners = new();
	private readonly WeakReference<DependencyObject>? _scopeParentElement;
	private AKInvokeReturnParams _lastInvokeResult;

	/// <summary>
	/// Creates a new scope with the given parent element and list of elements with access keys.
	/// </summary>
	internal AKScope(DependencyObject? scopeParentElement, List<(DependencyObject element, AKAccessKey accessKey)> initList)
	{
		_scopeParentElement = scopeParentElement is not null
			? new WeakReference<DependencyObject>(scopeParentElement)
			: null;

		foreach (var (element, accessKey) in initList)
		{
			var owner = new AKOwner(element, accessKey);
			_accessKeyOwners[accessKey] = owner;
		}
	}

	/// <summary>
	/// Gets the parent element of this scope.
	/// </summary>
	internal DependencyObject? GetScopeParent()
	{
		if (_scopeParentElement is null)
		{
			return null;
		}
		_scopeParentElement.TryGetTarget(out var parent);
		return parent;
	}

	/// <summary>
	/// Processes a character input and attempts to invoke a matching access key.
	/// </summary>
	/// <param name="inputCharacter">The character typed by the user.</param>
	/// <param name="allowPartialMatching">Whether to allow partial matching for visual feedback.</param>
	/// <param name="invokeResult">Result of the invoke attempt.</param>
	internal void Invoke(char inputCharacter, bool allowPartialMatching, out AKInvokeReturnParams invokeResult)
	{
		bool inputAccumulatorSurrogateFlag = _inputAccumulator.Length > 0 && char.IsHighSurrogate(_inputAccumulator[^1]);
		_inputAccumulator += inputCharacter;
		var keyToMatch = new AKAccessKey(_inputAccumulator);

		invokeResult = TryInvokeDirectMatch(keyToMatch);
		if (invokeResult.InvokeAttempted)
		{
			// Successfully found and invoked a match.
			_inputAccumulator = "";
			return;
		}
		else if (allowPartialMatching && HasPartialMatch(keyToMatch))
		{
			if (char.IsHighSurrogate(inputCharacter))
			{
				// We're in a middle of a surrogate pair, do not try to update the visual state in that case.
				invokeResult.InvokeAttempted = false;
				return;
			}

			// Have a partial match, update the visual state of the AccessKeys
			UpdatePartialMatchAccessKeyVisibility(keyToMatch);
			// Do not reset the input accumulator, return true (because partial match found).
			invokeResult.InvokeAttempted = true;
			return;
		}

		if (allowPartialMatching && inputAccumulatorSurrogateFlag && char.IsLowSurrogate(inputCharacter))
		{
			// If no direct or partial matches and in a middle of a surrogate pair matching,
			// make sure we remove the whole pair.
			_inputAccumulator = _inputAccumulator[..^1];
		}

		// No direct or partial matches.
		// Don't reset the inputAccumulator if filtering, but pop off the last character.
		if (_inputAccumulator.Length > 1)
		{
			_inputAccumulator = _inputAccumulator[..^1];
			invokeResult.InvokeAttempted = false;
			return;
		}
		else
		{
			_inputAccumulator = "";
			invokeResult.InvokeAttempted = false;
			return;
		}
	}

	/// <summary>
	/// Shows all access keys in the scope. Calls ShowAccessKey for each AccessKey owner in this scope.
	/// </summary>
	internal void ShowAccessKeys()
	{
		foreach (var pair in _accessKeyOwners)
		{
			pair.Value.ShowAccessKey(""); // pressed keys - empty string means no keys pressed
		}
	}

	/// <summary>
	/// Hides all access keys in the scope.
	/// </summary>
	internal void HideAccessKeys()
	{
		foreach (var pair in _accessKeyOwners)
		{
			pair.Value.HideAccessKey();
		}
	}

	/// <summary>
	/// Adds an element to the scope if it has a valid access key.
	/// </summary>
	internal void AddToAccessKeyOwner(DependencyObject element)
	{
		var accessString = Microsoft.UI.Xaml.Input.AccessKeys.GetAccessKey(element);
		if (string.IsNullOrEmpty(accessString))
		{
			return;
		}

		if (!AKParser.TryParseAccessKey(accessString, out var accessKey))
		{
			return;
		}

		var owner = new AKOwner(element, accessKey);

		if (!ContainsOwner(accessKey))
		{
			_accessKeyOwners[accessKey] = owner;
		}

		// We always fire the show when an element is added for scenarios where an element has
		// been added to the scope, but failed to fire. This can happen in nested flyout scenarios
		// where the flyout is visible in the tree, but has not fired yet.
		var keyToMatch = new AKAccessKey(_inputAccumulator);
		if (keyToMatch.IsPartialMatch(accessKey))
		{
			owner.ShowAccessKey(keyToMatch.GetAccessKeyString());
		}
	}

	/// <summary>
	/// Removes an element from the scope.
	/// </summary>
	internal void RemoveFromAccessKeyOwner(DependencyObject element)
	{
		var accessString = Microsoft.UI.Xaml.Input.AccessKeys.GetAccessKey(element);
		if (string.IsNullOrEmpty(accessString))
		{
			return;
		}

		if (!AKParser.TryParseAccessKey(accessString, out var accessKey))
		{
			return;
		}

		var keyToMatch = new AKAccessKey(_inputAccumulator);

		if (ContainsOwner(accessKey) && _accessKeyOwners.TryGetValue(accessKey, out var existingOwner))
		{
			// Make sure the element getting removed is the same one that we have in the accessKeyOwners
			// map. It could be that the caller is calling about an element that we're no longer tracking,
			// that just happens to have the same access key (bug 8455086).
			if (existingOwner.Element.TryGetTarget(out var existingElement) && ReferenceEquals(existingElement, element))
			{
				_accessKeyOwners.Remove(accessKey);

				if (keyToMatch.IsPartialMatch(accessKey))
				{
					existingOwner.HideAccessKey();
				}
			}
		}
	}

	/// <summary>
	/// Returns true if an element entering the tree should trigger a scope update.
	/// </summary>
	internal bool ShouldElementEnteringTreeUpdateScope(DependencyObject? scopeOwner)
	{
		if (_lastInvokeResult.InvokedElement is null || !_lastInvokeResult.InvokeAttempted)
		{
			return false;
		}

		return _lastInvokeResult.InvokedElement.TryGetTarget(out var lastInvoked) &&
			   ReferenceEquals(lastInvoked, scopeOwner);
	}

	/// <summary>
	/// Returns true if the scope is currently filtering input (has accumulated characters).
	/// </summary>
	internal bool IsScopeFilteringInput => !string.IsNullOrEmpty(_inputAccumulator);

	/// <summary>
	/// Processes the Escape key - backs off one character from the input accumulator.
	/// </summary>
	internal void ProcessEscapeKey()
	{
		if (IsScopeFilteringInput)
		{
			var poppedChar = _inputAccumulator[^1];
			_inputAccumulator = _inputAccumulator[..^1];
			if (_inputAccumulator.Length > 0 && char.IsSurrogatePair(_inputAccumulator[^1], poppedChar))
			{
				// Handle the surrogate pair case.
				_inputAccumulator = _inputAccumulator[..^1];
			}

			// Call show on the elements that have now been filtered in
			var keyToMatch = new AKAccessKey(_inputAccumulator);
			UpdatePartialMatchAccessKeyVisibility(keyToMatch);
		}
	}

	/// <summary>
	/// Tries to invoke a direct (exact) match for the given access key.
	/// </summary>
	private AKInvokeReturnParams TryInvokeDirectMatch(AKAccessKey inputKey)
	{
		var invokeResult = AKInvokeReturnParams.Default;
		invokeResult.InvokeAttempted = false;

		foreach (var pair in _accessKeyOwners)
		{
			var owner = pair.Value;

			if (owner.AccessKey == inputKey)
			{
				invokeResult.InvokeFoundValidPattern = owner.Invoke();
				invokeResult.InvokeAttempted = true;
				invokeResult.InvokedElement = owner.Element;

				_lastInvokeResult = invokeResult;
				return invokeResult;
			}
		}

		return invokeResult;
	}

	/// <summary>
	/// Returns true if a partial match has been found.
	/// </summary>
	private bool HasPartialMatch(AKAccessKey inputKey)
	{
		foreach (var pair in _accessKeyOwners)
		{
			if (inputKey.IsPartialMatch(pair.Value.AccessKey))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Updates the visibility of access keys based on partial matching.
	/// </summary>
	private void UpdatePartialMatchAccessKeyVisibility(AKAccessKey inputKey)
	{
		foreach (var pair in _accessKeyOwners)
		{
			var owner = pair.Value;

			// For partialMatches and non-matches, send a showAccessKey/HideAccessKey event to the owner.
			// This way visuals can be updated to reflect each key stroke.
			if (inputKey.IsPartialMatch(owner.AccessKey))
			{
				owner.ShowAccessKey(inputKey.GetAccessKeyString());
			}
			else
			{
				owner.HideAccessKey();
			}
		}
	}

	/// <summary>
	/// Returns true if the scope contains an owner with the given access key.
	/// </summary>
	private bool ContainsOwner(AKAccessKey accessKey)
	{
		return _accessKeyOwners.ContainsKey(accessKey);
	}
}
