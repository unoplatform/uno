// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference VirtualizingLayout.cpp, commit 4b206bce3

using System;
using System.Collections.Specialized;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class VirtualizingLayout
{
	// #pragma region IVirtualizingLayoutOverrides

	public VirtualizingLayout()
	{
		//__RP_Marker_ClassById(RuntimeProfiler.ProfId_VirtualizingLayout);
	}

	/// <summary>
	/// When implemented in a derived class, initializes any per-container state the layout requires
	/// when it is attached to a UIElement container.
	/// </summary>
	/// <param name="context">The context object that facilitates communication between the layout and its host container.</param>
	protected internal virtual void InitializeForContextCore(VirtualizingLayoutContext context)
	{
	}

	/// <summary>
	/// When implemented in a derived class, removes any state the layout previously stored on the
	/// UIElement container.
	/// </summary>
	/// <param name="context">The context object that facilitates communication between the layout and its host container.</param>
	protected internal virtual void UninitializeForContextCore(VirtualizingLayoutContext context)
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
	protected internal virtual Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
		=> throw new NotImplementedException();

	/// <summary>
	/// Provides the behavior for the "Arrange" pass of layout. Classes can override this method to
	/// define their own "Arrange" pass behavior.
	/// </summary>
	/// <param name="context">The context object that facilitates communication between the layout and its host container.</param>
	/// <param name="finalSize">The final size that the container computes for the child in layout.</param>
	/// <returns>The actual size that is used after the element is arranged in layout.</returns>
	protected internal virtual Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
	{
		// Do not throw. If the layout decides to arrange its
		// children during measure, then an ArrangeOverride is not required.
		return finalSize;
	}

	/// <summary>
	/// Notifies the layout when the data collection assigned to the ItemsRepeater changes.
	/// </summary>
	/// <param name="context">The context object that facilitates communication between the layout and its host container.</param>
	/// <param name="source">The data source.</param>
	/// <param name="args">The change arguments.</param>
	protected internal virtual void OnItemsChangedCore(VirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
		=> InvalidateMeasure();

	// #pragma endregion
}
