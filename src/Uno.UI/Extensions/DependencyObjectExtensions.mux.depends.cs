// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// depends.h, depends.cpp

#nullable enable

using Uno.UI.Xaml.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;

namespace Uno.UI.Extensions
{
	internal static partial class DependencyObjectExtensions
	{
		internal static VisualTree? GetVisualTree(this DependencyObject dependencyObject)
		{
			// The current implementation does not match MUX https://github.com/unoplatform/uno/issues/8978
			if (dependencyObject is UIElement uiElement)
			{
				return uiElement.VisualTreeCache;
			}

			if (dependencyObject is FlyoutBase flyoutBase)
			{
				return flyoutBase.VisualTreeCache;
			}

			if (dependencyObject is TextElement textElement)
			{
				return textElement.VisualTreeCache;
			}

			return null;
		}

		internal static void SetVisualTree(this DependencyObject dependencyObject, VisualTree visualTree)
		{
			// The current implementation does not match MUX https://github.com/unoplatform/uno/issues/8978
			if (dependencyObject is UIElement uiElement)
			{
				uiElement.VisualTreeCache = visualTree;
			}

			if (dependencyObject is FlyoutBase flyoutBase)
			{
				flyoutBase.VisualTreeCache = visualTree;
			}

			if (dependencyObject is TextElement textElement)
			{
				textElement.VisualTreeCache = visualTree;
			}
		}
	}
}
