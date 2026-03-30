// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\TreeAnalyzer.h, tag winui3/release/1.4.3, commit 685d2bf

#if __SKIA__
#nullable enable

using System.Collections.Generic;
using Uno.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Input;

/// <summary>
/// Visual tree walker that finds elements with valid AccessKey properties.
/// </summary>
internal sealed class AKTreeAnalyzer
{
	private const int MaxDepth = 200;

	private readonly AKVisualTreeFinder _treeLibrary;

	internal AKTreeAnalyzer(AKVisualTreeFinder treeLibrary)
	{
		_treeLibrary = treeLibrary;
	}

	/// <summary>
	/// Finds all elements in the scope of scopeOwner that have a valid AccessKey property.
	/// </summary>
	internal void FindElementsForAK(DependencyObject? scopeOwner, List<DependencyObject> elementList, bool returnOnFirstHit = false)
	{
		ValidateScopeOwner(scopeOwner);

		var scopeOwnerMap = new List<(DependencyObject ScopeOwner, DependencyObject Child)>();
		BuildScopeOwnerMap(scopeOwnerMap);

		// If we don't have an element (scopeOwner = null), we're conceptually looking in the "root scope".
		// This means we look at all the visual roots and gather the access keys.
		if (IsRootScope(scopeOwner))
		{
			var roots = new List<DependencyObject>();
			_treeLibrary.GetAllVisibleRoots(roots);
			foreach (var root in roots)
			{
				WalkTreeAndFindElements(root, root, scopeOwnerMap, elementList, returnOnFirstHit);
			}
		}
		else
		{
			WalkTreeAndFindElements(scopeOwner!, scopeOwner!, scopeOwnerMap, elementList, returnOnFirstHit);
		}
	}

	/// <summary>
	/// Returns true if the visual tree contains any element with a valid AccessKey.
	/// </summary>
	internal bool DoesTreeContainAKElement()
	{
		var elementList = new List<DependencyObject>();
		// Passing null as scopeOwner to search in all visual roots
		FindElementsForAK(null, elementList, true);
		return elementList.Count > 0;
	}

	/// <summary>
	/// Gets the scope owner for an element by walking up the tree.
	/// </summary>
	internal DependencyObject? GetScopeOwner(DependencyObject e)
	{
		if (AKVisualTreeFinder.IsScope(e))
		{
			var parent = _treeLibrary.GetParent(e);
			// TODO Uno: C++ also checks GetMentor() for elements like Flyouts that may have a mentor but no parent.
			e = parent!;
		}

		return GetScope(e);
	}

	internal bool IsScopeOwner(DependencyObject current) => AKVisualTreeFinder.IsScope(current);

	internal bool IsValidAKElement(DependencyObject element)
	{
		return IsAccessKey(element)
			&& FocusProperties.IsVisible(element)
			&& FocusProperties.AreAllAncestorsVisible(element)
			&& FocusProperties.IsEnabled(element);
	}

	internal bool IsAccessKey(DependencyObject element)
	{
		return !string.IsNullOrEmpty(AccessKeys.GetAccessKey(element));
	}

	private void BuildScopeOwnerMap(List<(DependencyObject, DependencyObject)> scopeOwnerMap)
	{
		var roots = new List<DependencyObject>();
		_treeLibrary.GetAllVisibleRoots(roots);
		foreach (var root in roots)
		{
			BuildScopeOwnerMapImpl(root, scopeOwnerMap);
		}
	}

	private void BuildScopeOwnerMapImpl(DependencyObject? current, List<(DependencyObject, DependencyObject)> scopeOwnerMap)
	{
		if (current is null)
		{
			return;
		}

		var scopeOwner = _treeLibrary.GetScopeOwner(current);
		if (scopeOwner is not null)
		{
			// The scopeOwner must be a scope itself
			scopeOwnerMap.Add((scopeOwner, current));
		}

		var children = _treeLibrary.GetChildren(current);
		if (children is not null)
		{
			for (int i = 0; i < children.Count; i++)
			{
				BuildScopeOwnerMapImpl(children[i], scopeOwnerMap);
			}
		}
	}

	private void WalkTreeAndFindElements(
		DependencyObject startRoot,
		DependencyObject currentElement,
		List<(DependencyObject ScopeOwner, DependencyObject Child)> scopeOwnerMap,
		List<DependencyObject> elementList,
		bool returnOnFirstHit = false,
		int depth = MaxDepth)
	{
		if (depth == 0)
		{
			// Possible cycle
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
			var children = _treeLibrary.GetChildren(currentElement);
			if (children is not null)
			{
				for (int i = 0; i < children.Count; i++)
				{
					var child = children[i];
					var owner = _treeLibrary.GetScopeOwner(child);
					// If owner is null that shows that no specific scope owner is defined for this child element
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
				if (ReferenceEquals(entry.ScopeOwner, currentElement))
				{
					WalkTreeAndFindElements(startRoot, entry.Child, scopeOwnerMap, elementList, returnOnFirstHit, depth - 1);
					if (returnOnFirstHit && elementList.Count > 0)
					{
						return;
					}
				}
			}
		}
	}

	private DependencyObject? GetScope(DependencyObject? e)
	{
		// If we're visiting too many nodes during the walk, we probably found a cycle.
		int iterations = MaxDepth;
		while (iterations-- != 0)
		{
			if (e is null)
			{
				// We walked up through the root of the tree. Consider null the root scope.
				return null;
			}

			if (AKVisualTreeFinder.IsScope(e))
			{
				return e;
			}

			var owner = _treeLibrary.GetScopeOwner(e);
			if (owner is not null)
			{
				e = owner;
			}
			else
			{
				e = _treeLibrary.GetParent(e);
			}
		}

		// Tree is unexpectedly deep, or we hit a cycle somehow.
		// In C++ this is XAML_FAIL_FAST(). We return null instead to be safe.
		return null;
	}

	private static bool IsRootScope(DependencyObject? scopeOwner) => scopeOwner is null;

	private void ValidateScopeOwner(DependencyObject? scopeOwner)
	{
		// Element must be a scope if not null
		if (scopeOwner is not null && !IsRootScope(scopeOwner))
		{
			// In C++ this asserts. We just proceed.
		}
	}
}
#endif
