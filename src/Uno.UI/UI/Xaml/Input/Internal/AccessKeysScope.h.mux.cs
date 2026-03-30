// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\Scope.h, tag winui3/release/1.4.3, commit 685d2bf

#if __SKIA__
#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Input;

/// <summary>
/// Represents a set of Access Key owners of which any could be invoked.
/// Checks current input fed from the ScopeTree for matching keys and invokes on a match.
/// </summary>
internal sealed class AKScope
{
	private string _inputAccumulator = string.Empty;

	// To guard against reentrancy, we should always take a copy of the owner instead of a reference when iterating.
	private readonly Dictionary<AKAccessKey, AKOwner> _accessKeyOwners = new();
	private readonly WeakReference<DependencyObject>? _scopeParentElement;
	private AccessKeys.AKInvokeReturnParams _lastInvokeResult;

	internal AKScope(DependencyObject? scopeParentElement, List<(DependencyObject Element, AKAccessKey AccessKey)> initList)
	{
		_scopeParentElement = scopeParentElement is not null
			? new WeakReference<DependencyObject>(scopeParentElement)
			: null;

		foreach (var (element, accessKey) in initList)
		{
			if (!_accessKeyOwners.ContainsKey(accessKey))
			{
				_accessKeyOwners[accessKey] = new AKOwner(element, accessKey);
			}
		}
	}

	internal void Invoke(char inputCharacter, bool allowPartialMatching, out AccessKeys.AKInvokeReturnParams invokeResult)
	{
		var inputAccumulatorSurrogateFlag = _inputAccumulator.Length > 0 && char.IsHighSurrogate(_inputAccumulator[^1]);
		_inputAccumulator += inputCharacter;
		var keyToMatch = new AKAccessKey(_inputAccumulator);

		invokeResult = TryInvokeDirectMatch(keyToMatch);
		if (invokeResult.InvokeAttempted)
		{
			// Successfully found and invoked a match.
			_inputAccumulator = string.Empty;
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
			// Do not reset the input accumulator, return true (because partial match - scope is likely still valid).
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
		}
		else
		{
			_inputAccumulator = string.Empty;
			invokeResult.InvokeAttempted = false;
		}
	}

	/// <summary>
	/// Shows all access keys in the scope. Calls ShowAccessKey for each AccessKey owner in this scope.
	/// </summary>
	internal void ShowAccessKeys()
	{
		foreach (var pair in _accessKeyOwners)
		{
			pair.Value.ShowAccessKey(string.Empty /* pressed keys - empty string means no keys pressed */);
		}
	}

	internal void HideAccessKeys()
	{
		foreach (var pair in _accessKeyOwners)
		{
			pair.Value.HideAccessKey();
		}
	}

	internal void AddToAccessKeyOwner(DependencyObject element)
	{
		var accessString = AccessKeys.GetAccessKey(element);
		var accessKey = new AKAccessKey(accessString);
		var owner = new AKOwner(element, accessKey);

		if (!string.IsNullOrEmpty(accessString) && !_accessKeyOwners.ContainsKey(accessKey))
		{
			_accessKeyOwners[accessKey] = owner;
		}

		// We always fire the show when an element is added for scenarios where an element has
		// been added to the scope, but failed to fire. This can happen in nested flyout scenarios
		// where the flyout is visible in the tree, but has not fired yet.
		var keyToMatch = new AKAccessKey(_inputAccumulator);
		if (keyToMatch.IsPartialMatch(owner.GetAccessKey()))
		{
			owner.ShowAccessKey(keyToMatch.GetAccessKeyString());
		}
	}

	internal void RemoveFromAccessKeyOwner(DependencyObject element)
	{
		var accessString = AccessKeys.GetAccessKey(element);
		var accessKey = new AKAccessKey(accessString);
		var keyToMatch = new AKAccessKey(_inputAccumulator);

		if (!string.IsNullOrEmpty(accessString) && _accessKeyOwners.TryGetValue(accessKey, out var existingOwner))
		{
			// Make sure the element getting removed is the same one that we have in the accessKeyOwners
			// map. It could be that the caller is calling about an element that we're no longer tracking,
			// that just happens to have the same access key (true story! bug 8455086)
			if (existingOwner.GetElement().TryGetTarget(out var ownerElement) && ReferenceEquals(ownerElement, element))
			{
				_accessKeyOwners.Remove(accessKey);

				if (keyToMatch.IsPartialMatch(accessKey))
				{
					existingOwner.HideAccessKey();
				}
			}
		}
	}

	internal bool ShouldElementEnteringTreeUpdateScope(DependencyObject? scopeOwner)
	{
		if (_lastInvokeResult.InvokedElement is not null &&
			_lastInvokeResult.InvokeAttempted &&
			_lastInvokeResult.InvokedElement.TryGetTarget(out var invokedElement))
		{
			return ReferenceEquals(invokedElement, scopeOwner);
		}

		return false;
	}

	internal DependencyObject? GetScopeParent()
	{
		if (_scopeParentElement is not null && _scopeParentElement.TryGetTarget(out var parent))
		{
			return parent;
		}

		return null;
	}

	internal WeakReference<DependencyObject>? GetScopeParentWeakRef() => _scopeParentElement;

	internal bool IsScopeFilteringInput() => !string.IsNullOrEmpty(_inputAccumulator);

	internal void ProcessEscapeKey()
	{
		if (IsScopeFilteringInput())
		{
			var poppedChar = _inputAccumulator[^1];
			_inputAccumulator = _inputAccumulator[..^1];
			if (_inputAccumulator.Length > 0 && char.IsHighSurrogate(_inputAccumulator[^1]) && char.IsLowSurrogate(poppedChar))
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
	/// Returns result when a match was found and invoked, otherwise default.
	/// </summary>
	private AccessKeys.AKInvokeReturnParams TryInvokeDirectMatch(AKAccessKey inputKey)
	{
		var invokeResult = new AccessKeys.AKInvokeReturnParams();
		invokeResult.InvokeAttempted = false;
		invokeResult.InvokeFoundValidPattern = true;

		// Take a snapshot to guard against reentrancy during invoke
		var snapshot = new List<KeyValuePair<AKAccessKey, AKOwner>>(_accessKeyOwners);

		foreach (var pair in snapshot)
		{
			var owner = pair.Value;

			if (owner.GetAccessKey() == inputKey)
			{
				invokeResult.InvokeFoundValidPattern = owner.Invoke();
				invokeResult.InvokeAttempted = true;
				invokeResult.InvokedElement = owner.GetElement();

				_lastInvokeResult = invokeResult;
				return invokeResult;
			}
		}

		return invokeResult;
	}

	/// <summary>
	/// Returns true if a partial match has been found. A partial match means that the key sequence entered thus far
	/// matches the starting key sequence of at least one AKOwner's AKAccessKey.
	/// </summary>
	private bool HasPartialMatch(AKAccessKey inputKey)
	{
		foreach (var pair in _accessKeyOwners)
		{
			if (inputKey.IsPartialMatch(pair.Value.GetAccessKey()))
			{
				return true;
			}
		}

		return false;
	}

	private void UpdatePartialMatchAccessKeyVisibility(AKAccessKey inputKey)
	{
		// Take a snapshot to guard against reentrancy
		var snapshot = new List<KeyValuePair<AKAccessKey, AKOwner>>(_accessKeyOwners);

		foreach (var pair in snapshot)
		{
			var owner = pair.Value;

			// For partialMatches and non-matches, send a showAccessKey/HideAccessKey event to the owner.
			// This way visuals can be updated to reflect each key stroke (both positive match feedback, and negative match feedback).
			if (inputKey.IsPartialMatch(owner.GetAccessKey()))
			{
				owner.ShowAccessKey(inputKey.GetAccessKeyString());
			}
			else
			{
				owner.HideAccessKey();
			}
		}
	}
}
#endif
