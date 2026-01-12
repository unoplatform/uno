// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\ScopeBuilder.h, tag winui3/release/1.5.3

#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using AKCommon = Microsoft.UI.Xaml.Input.AccessKeys;

namespace Uno.UI.Xaml.Input.AccessKeys;

/// <summary>
/// Builds access key scopes by analyzing the visual tree and collecting elements with access keys.
/// </summary>
internal class AKScopeBuilder
{
	private readonly AKTreeAnalyzer _treeAnalyzer;

	internal AKScopeBuilder(AKTreeAnalyzer treeAnalyzer)
	{
		_treeAnalyzer = treeAnalyzer;
	}

	/// <summary>
	/// Constructs a new scope for the given parent element.
	/// Returns the scope if it was created, otherwise returns null.
	/// </summary>
	/// <param name="parentElementForNewScope">The parent element for the new scope, or null for root scope.</param>
	/// <returns>The constructed scope, or null if no valid elements were found.</returns>
	internal AKScope? ConstructScope(DependencyObject? parentElementForNewScope)
	{
		// We want to figure out which scope we are currently on and attempt to build the next scope.
		// Once we've determined the UI Element that represent that scope, fetch all the eligible elements, match the elements
		// with the appropriate access key based on the information from the parser, and then create the scope

		var elementsForNewScope = new List<DependencyObject>();
		GetElementsForAKScope(parentElementForNewScope, elementsForNewScope);

		// A scope cannot exist without an AKO. If the scope init list is empty, that means that this scope is invalid
		if (elementsForNewScope.Count == 0)
		{
			return null;
		}

		var scopeInitList = new List<(DependencyObject element, AKAccessKey accessKey)>(elementsForNewScope.Count);

		foreach (var element in elementsForNewScope)
		{
			var accessString = AKCommon.GetAccessKey(element);

			bool succeeded = AKParser.TryParseAccessKey(accessString, out var accessKey);

			// We only want to add to the init list if the parsing of the element was successful
			if (succeeded)
			{
				// A scope needs a list of all the valid AccessKeys. It uses this in order to create all the AKOs
				scopeInitList.Add((element, accessKey));
			}
		}

		if (scopeInitList.Count == 0)
		{
			return null;
		}

		// Create the new scope
		return new AKScope(parentElementForNewScope, scopeInitList);
	}

	private void GetElementsForAKScope(DependencyObject? scopeOwner, List<DependencyObject> elementList)
	{
		_treeAnalyzer.FindElementsForAK(scopeOwner, elementList);
	}
}
