// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// depends.h, depends.cpp

#nullable enable

using Uno.UI.Xaml.Core;
using Windows.UI.Xaml;

namespace Uno.UI.Extensions
{
	internal static partial class DependencyObjectExtensions
	{
		internal static VisualTree? GetVisualTree(this DependencyObject dependencyObject)
		{
			//TODO:MZ: This should work for non-UIElement as well!
			if (dependencyObject is UIElement uiElement)
			{
				return uiElement.VisualTree;
			}

			return null;
		}

		internal static void SetVisualTree(this DependencyObject dependencyObject, VisualTree visualTree)
		{
			//TODO:MZ: This should work for non-UIElement as well!
			if (dependencyObject is UIElement uiElement)
			{
				uiElement.VisualTree = visualTree;
			}
		}
	}
}
