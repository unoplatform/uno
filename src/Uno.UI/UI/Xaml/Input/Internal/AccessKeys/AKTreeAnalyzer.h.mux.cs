// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\TreeAnalyzer.h, tag winui3/release/1.5.3

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Uno.UI.Helpers.WinUI;

namespace Uno.UI.Xaml.Input.AccessKeys;

/// <summary>
/// Analyzes the visual tree to find elements with access keys.
/// </summary>
internal class AKTreeAnalyzer
{
	private const int MaxDepth = 200;
	private readonly AKVisualTreeFinder _treeLibrary;

	internal AKTreeAnalyzer(AKVisualTreeFinder treeLibrary)
	{
		_treeLibrary = treeLibrary;
	}

	/// <summary>
	/// Finds all elements with access keys in the given scope.
	/// </summary>
	/// <param name="scopeOwner">The scope owner element, or null for root scope.</param>
	/// <param name="elementList">List to populate with found elements.</param>
	/// <param name="returnOnFirstHit">If true, returns after finding the first element.</param>
	internal void FindElementsForAK(DependencyObject? scopeOwner, List<DependencyObject> elementList, bool returnOnFirstHit = false)
	{
		ValidateScopeOwner(scopeOwner);

		var scopeOwnerMap = new List<(DependencyObject scopeOwner, DependencyObject element)>();
		BuildScopeOwnerMap(scopeOwnerMap);

		// If we don't have an element i.e. scopeOwner = null, we're conceptually looking in the "root scope".
		// This means we look at all the visual roots and gather the access keys.
		if (IsRootScope(scopeOwner))
		{
			var roots = new DependencyObject?[3];
			_treeLibrary.GetAllVisibleRootsNoRef(roots);
			foreach (var root in roots)
			{
				if (root is not null)
				{
					WalkTreeAndFindElements(root, root, scopeOwnerMap, elementList, returnOnFirstHit, MaxDepth);
					if (returnOnFirstHit && elementList.Count > 0)
					{
						return;
					}
				}
			}
		}
		else
		{
			WalkTreeAndFindElements(scopeOwner!, scopeOwner!, scopeOwnerMap, elementList, returnOnFirstHit, MaxDepth);
		}
	}

	/// <summary>
	/// Checks if the tree contains any element with an access key.
	/// </summary>
	internal bool DoesTreeContainAKElement()
	{
		var elementList = new List<DependencyObject>();
		// passing null as scopeOwner to search in all visual roots
		FindElementsForAK(null, elementList, true);
		return elementList.Count > 0;
	}

	/// <summary>
	/// Gets the scope owner for an element.
	/// </summary>
	internal DependencyObject? GetScopeOwner(DependencyObject element)
	{
		if (AKVisualTreeFinder.IsScope(element))
		{
			var parent = _treeLibrary.GetParent(element);
			if (parent is null)
			{
				// Some elements, like Flyouts, may have a mentor but no parent.
				parent = _treeLibrary.GetMentor(element);
			}
			element = parent ?? element;
		}

		return GetScope(element);
	}

	/// <summary>
	/// Returns true if the element is a scope owner (IsAccessKeyScope).
	/// </summary>
	internal bool IsScopeOwner(DependencyObject element)
	{
		return AKVisualTreeFinder.IsScope(element);
	}

	/// <summary>
	/// Returns true if the element is a valid access key element (has key, visible, enabled).
	/// </summary>
	internal bool IsValidAKElement(DependencyObject element)
	{
		return IsAccessKey(element) && IsVisible(element) && AreAllAncestorsVisible(element) && IsEnabled(element);
	}

	/// <summary>
	/// Returns true if the element has an access key defined.
	/// </summary>
	internal bool IsAccessKey(DependencyObject element)
	{
		var accessKey = Microsoft.UI.Xaml.Input.AccessKeys.GetAccessKey(element);
		return !string.IsNullOrEmpty(accessKey);
	}

	/// <summary>
	/// Builds a map of scope owners to elements that have explicit AccessKeyScopeOwner set.
	/// </summary>
	private void BuildScopeOwnerMap(List<(DependencyObject scopeOwner, DependencyObject element)> scopeOwnerMap)
	{
		var roots = new DependencyObject?[3];
		_treeLibrary.GetAllVisibleRootsNoRef(roots);
		foreach (var root in roots)
		{
			if (root is not null)
			{
				BuildScopeOwnerMapImpl(root, scopeOwnerMap);
			}
		}
	}

	private void BuildScopeOwnerMapImpl(DependencyObject? current, List<(DependencyObject scopeOwner, DependencyObject element)> scopeOwnerMap)
	{
		if (current is null)
		{
			return;
		}

		var scopeOwner = _treeLibrary.GetScopeOwner(current);
		if (scopeOwner is not null)
		{
			// The scopeOwner must be a scope itself
			if (!AKVisualTreeFinder.IsScope(scopeOwner))
			{
				// Invalid state - scopeOwner should be a scope
				return;
			}
			scopeOwnerMap.Add((scopeOwner, current));
		}

		var collection = _treeLibrary.GetChildren(current);
		if (collection is not null)
		{
			foreach (var child in collection)
			{
				BuildScopeOwnerMapImpl(child, scopeOwnerMap);
			}
		}
	}

	private void WalkTreeAndFindElements(
		DependencyObject startRoot,
		DependencyObject currentElement,
		List<(DependencyObject scopeOwner, DependencyObject element)> scopeOwnerMap,
		List<DependencyObject> elementList,
		bool returnOnFirstHit,
		int depth)
	{
		if (depth == 0)
		{
			// Possible cycle - prevent stack overflow
			return;
		}

		// If this element doesn't represent the scope, and it has an access key,
		// add the access key to the list
		if (!ReferenceEquals(startRoot, currentElement) && IsValidAKElement(currentElement))
		{
			elementList.Add(currentElement);

			// We want to find if the tree has AK elements. If it does, there is no need to search any further.
			if (returnOnFirstHit)
			{
				return;
			}
		}

		// If we hit a new scope root, we've hit the edge of the current scope.
		// Don't walk the children.
		bool isNewScope = !ReferenceEquals(startRoot, currentElement) && AKVisualTreeFinder.IsScope(currentElement);
		if (!isNewScope)
		{
			var collection = _treeLibrary.GetChildren(currentElement);
			if (collection is not null)
			{
				foreach (var child in collection)
				{
					var owner = _treeLibrary.GetScopeOwner(child);
					// if owner is null that shows that no specific scope owner is defined for this child element
					// the start root can be considered as the scope for finding access keys
					if (owner is null)
					{
						WalkTreeAndFindElements(startRoot, child, scopeOwnerMap, elementList, returnOnFirstHit, depth - 1);
						if (returnOnFirstHit && elementList.Count > 0)
						{
							return;
						}
					}
				}
			}

			// Find the children explicitly grafted to this scope
			foreach (var entry in scopeOwnerMap)
			{
				if (ReferenceEquals(entry.scopeOwner, currentElement))
				{
					WalkTreeAndFindElements(startRoot, entry.element, scopeOwnerMap, elementList, returnOnFirstHit, depth - 1);
					if (returnOnFirstHit && elementList.Count > 0)
					{
						return;
					}
				}
			}
		}
	}

	private DependencyObject? GetScope(DependencyObject? element)
	{
		// If we're visiting too many nodes during the walk, we probably found a cycle.
		// Keep track of iterations so we don't loop forever.
		int iterations = MaxDepth;
		while (iterations-- != 0)
		{
			if (element is null)
			{
				// We walked up through the root of the tree. Consider
				// null the root scope.
				return null;
			}

			if (AKVisualTreeFinder.IsScope(element))
			{
				return element;
			}

			var owner = _treeLibrary.GetScopeOwner(element);
			if (owner is not null)
			{
				element = owner;
			}
			else
			{
				element = _treeLibrary.GetParent(element);
			}
		}

		// Tree is unexpectedly deep, or we hit a cycle somehow.
		// Return null to avoid infinite loop.
		return null;
	}

	private static bool IsRootScope(DependencyObject? scopeOwner)
	{
		return scopeOwner is null;
	}

	private void ValidateScopeOwner(DependencyObject? scopeOwner)
	{
		if (scopeOwner is not null && !IsRootScope(scopeOwner))
		{
			// Element must be a scope
			if (!AKVisualTreeFinder.IsScope(scopeOwner))
			{
				throw new InvalidOperationException("Scope owner must be an access key scope.");
			}
		}
	}

	// Helper methods for visibility and enabled state

	private static bool IsVisible(DependencyObject element)
	{
		if (element is UIElement uiElement)
		{
			return uiElement.Visibility == Visibility.Visible;
		}
		return true; // TextElements don't have visibility
	}

	private static bool AreAllAncestorsVisible(DependencyObject element)
	{
		// Check visibility up the tree
		var current = element;
		while (current is not null)
		{
			if (current is UIElement uiElement && uiElement.Visibility != Visibility.Visible)
			{
				return false;
			}
			current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current as UIElement);
		}
		return true;
	}

	private static bool IsEnabled(DependencyObject element)
	{
		if (element is Control control)
		{
			return control.IsEnabled;
		}
		return true; // Non-controls are considered enabled
	}
}
