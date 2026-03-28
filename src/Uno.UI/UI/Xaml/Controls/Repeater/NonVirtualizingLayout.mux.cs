// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NonVirtualizingLayout.cpp, commit 5f9e851133b3

using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class NonVirtualizingLayout
{
	// #pragma region INonVirtualizingLayoutOverrides

	/// <summary>
	/// Called at the start of the layout cycle to allow the layout to initialize any per-container state.
	/// </summary>
	/// <param name="context">The context object that facilitates communication between the layout and its host container.</param>
	protected internal virtual void InitializeForContextCore(LayoutContext context)
	{
	}

	/// <summary>
	/// Called at the end of the layout cycle to allow the layout to clean up any per-container state.
	/// </summary>
	/// <param name="context">The context object that facilitates communication between the layout and its host container.</param>
	protected internal virtual void UninitializeForContextCore(LayoutContext context)
	{
	}

	/// <summary>
	/// Provides the behavior for the 'Measure' pass of the layout cycle.
	/// </summary>
	/// <param name="context">The context object that facilitates communication between the layout and its host container.</param>
	/// <param name="availableSize">The available space that a container can allocate to a child object.</param>
	/// <returns>The size that this object determines it needs during layout.</returns>
	protected internal virtual Size MeasureOverride(NonVirtualizingLayoutContext context, Size availableSize)
		=> throw new NotImplementedException();

	/// <summary>
	/// Provides the behavior for the 'Arrange' pass of the layout cycle.
	/// </summary>
	/// <param name="context">The context object that facilitates communication between the layout and its host container.</param>
	/// <param name="finalSize">The final size that the container computes for the child in layout.</param>
	/// <returns>The actual size that is used after the element is arranged in layout.</returns>
	protected internal virtual Size ArrangeOverride(NonVirtualizingLayoutContext context, Size finalSize)
		=> throw new NotImplementedException();

	// #pragma endregion
}
