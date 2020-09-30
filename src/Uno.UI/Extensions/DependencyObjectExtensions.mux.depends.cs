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
			//TODO Uno: Implement multi-root support, currently we use just a single visual tree
			//and all DOs return the same tree.
			return Uno.UI.Xaml.Core.CoreServices.Instance.MainRootVisual?.AssociatedVisualTree;
		}

		internal static void SetVisualTree(this DependencyObject dependencyObject, VisualTree visualTree)
		{
			//TODO Uno: Should allow setting the visual tree and be set when
			//the DO becomes part of the visual tree.
		}
	}
}
