// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NonvirtualizingLayoutContext.cpp, commit 5f9e851133b3

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class NonVirtualizingLayoutContext
{
	// #pragma region INonVirtualizingLayoutContext

	/// <summary>
	/// Gets the collection of child <see cref="UIElement"/>s from the container that provides the context.
	/// </summary>
	/// <value>The collection of child UIElements from the container that provides the context.</value>
	public IReadOnlyList<UIElement> Children => ChildrenCore;

	// #pragma endregion

	// #pragma region INonVirtualizingLayoutContextOverrides

	/// <summary>
	/// Implements the behavior for getting the return value of <see cref="Children"/> in a derived or custom NonVirtualizingLayoutContext.
	/// </summary>
	/// <value>The collection of child UIElements.</value>
	public virtual IReadOnlyList<UIElement> ChildrenCore => throw new NotSupportedException();

	// #pragma endregion

	internal VirtualizingLayoutContext GetVirtualizingContextAdapter()
	{
		if (m_contextAdapter == null)
		{
			m_contextAdapter = new LayoutContextAdapter(this);
		}

		return m_contextAdapter;
	}
}
