// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\ScopeBuilder.h, tag winui3/release/1.4.3, commit 685d2bf

#if __SKIA__
#nullable enable

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Input;

/// <summary>
/// Factory that constructs an AKScope from the visual tree.
/// </summary>
internal sealed class AKScopeBuilder
{
	private readonly AKTreeAnalyzer _treeAnalyzer;

	internal AKScopeBuilder(AKTreeAnalyzer treeAnalyzer)
	{
		_treeAnalyzer = treeAnalyzer;
	}

	/// <summary>
	/// Returns the scope if it was created, otherwise returns null.
	/// </summary>
	internal AKScope? ConstructScope(DependencyObject? parentElementForNewScope)
	{
		// We want to figure out which scope we are currently on and attempt to build the next scope.
		// Once we've determined the UI Element that represents that scope, fetch all the eligible elements,
		// match the elements with the appropriate access key based on the information from the parser,
		// and then create the scope.

		var elementsForNewScope = new List<DependencyObject>();
		_treeAnalyzer.FindElementsForAK(parentElementForNewScope, elementsForNewScope);

		// A scope cannot exist without an AKO. If the scope init list is empty, that means that this scope is invalid.
		if (elementsForNewScope.Count == 0)
		{
			return null;
		}

		var scopeInitList = new List<(DependencyObject Element, AKAccessKey AccessKey)>(elementsForNewScope.Count);

		foreach (var element in elementsForNewScope)
		{
			var accessString = AccessKeys.GetAccessKey(element);

			if (AKParser.TryParseAccessKey(accessString, out var accessKey))
			{
				// A scope needs a list of all the valid AccessKeys. It uses this in order to create all the AKOs.
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
}
#endif
