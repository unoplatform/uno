// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// depends.h, depends.cpp

#nullable enable

using Uno.UI.Xaml.Core;
using Microsoft.UI.Xaml;

namespace Uno.UI.Extensions
{
	internal static partial class DependencyObjectExtensions
	{
		// Matches WinUI's CDependencyObject::GetVisualTree / CDependencyObject::SetVisualTree.
		// In WinUI, every CDependencyObject has an m_pVisualTree field.
		// In Uno, this is stored on DependencyObjectStore so all DependencyObject types get it.

		internal static VisualTree? GetVisualTree(this DependencyObject dependencyObject)
		{
			if (dependencyObject is IDependencyObjectStoreProvider provider)
			{
				return provider.Store.VisualTreeCache;
			}

			return null;
		}

		internal static void SetVisualTree(this DependencyObject dependencyObject, VisualTree visualTree)
		{
			if (dependencyObject is IDependencyObjectStoreProvider provider)
			{
				provider.Store.VisualTreeCache = visualTree;
			}
		}
	}
}
