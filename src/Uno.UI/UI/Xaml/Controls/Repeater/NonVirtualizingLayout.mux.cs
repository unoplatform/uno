// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NonVirtualizingLayout.cpp, commit 4b206bce3

using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class NonVirtualizingLayout
{
	// #pragma region INonVirtualizingLayoutOverrides

	public NonVirtualizingLayout()
	{
		// TODO Uno: RuntimeProfiler marker not ported.
		// Original C++: __RP_Marker_ClassById(RuntimeProfiler::ProfId_NonVirtualizingLayout);
	}

	/// <summary>
	/// When implemented in a derived class, initializes any per-container state the layout requires
	/// when it is attached to a UIElement container.
	/// </summary>
	/// <param name="context">The context object that facilitates communication between the layout and its host container.</param>
	protected internal virtual void InitializeForContextCore(LayoutContext context)
	{
	}

	/// <summary>
	/// When implemented in a derived class, removes any state the layout previously stored on the
	/// UIElement container.
	/// </summary>
	/// <param name="context">The context object that facilitates communication between the layout and its host container.</param>
	protected internal virtual void UninitializeForContextCore(LayoutContext context)
	{
	}

	/// <summary>
	/// Provides the behavior for the "Measure" pass of layout. Classes can override this method to
	/// define their own "Measure" pass behavior.
	/// </summary>
	/// <param name="context">The context object that facilitates communication between the layout and its host container.</param>
	/// <param name="availableSize">The available space that a container can allocate to a child object.</param>
	/// <returns>The size that this object determines it needs during layout, based on its calculations
	/// of the allocated sizes for child objects or based on other considerations such as a fixed container size.</returns>
	protected internal virtual Size MeasureOverride(NonVirtualizingLayoutContext context, Size availableSize)
		=> throw new NotImplementedException();

	/// <summary>
	/// Provides the behavior for the "Arrange" pass of layout. Classes can override this method to
	/// define their own "Arrange" pass behavior.
	/// </summary>
	/// <param name="context">The context object that facilitates communication between the layout and its host container.</param>
	/// <param name="finalSize">The final size that the container computes for the child in layout.</param>
	/// <returns>The actual size that is used after the element is arranged in layout.</returns>
	protected internal virtual Size ArrangeOverride(NonVirtualizingLayoutContext context, Size finalSize)
		=> throw new NotImplementedException();

	// #pragma endregion
}
