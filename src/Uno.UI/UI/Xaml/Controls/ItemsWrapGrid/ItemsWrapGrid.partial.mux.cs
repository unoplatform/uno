// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//  Abstract:
//      Represents the implementation of our wrapping panel.
//      This is only a configuration class. All work actually happens in
//      strategy that is separately defined. We do this for testability
//      and flexibility in layering.

// MUX Reference ItemsWrapGrid_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d


#nullable enable

using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class ItemsWrapGrid
{
	public ItemsWrapGrid()
	{
		Initialize();
	}

	private void Initialize()
	{
		WrappingLayoutStrategy spWrapping;

		spWrapping = new WrappingLayoutStrategy();
		SetLayoutStrategyBase(spWrapping);
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == OrientationProperty)
		{
			Orientation orientation = (Orientation)args.NewValue;
			Orientation oldOrientation = (Orientation)args.OldValue;

			if (orientation != oldOrientation)
			{
				ILayoutStrategy spWrapping = GetLayoutStrategy();

				CacheFirstVisibleElementBeforeOrientationChange();

				// IWG orientation is the stacking direction. Which is the opposite of the
				// virtualization direction.
				if (orientation == Orientation.Horizontal)
				{
					((WrappingLayoutStrategy)spWrapping).SetVirtualizationDirection(Orientation.Vertical);
				}
				else
				{
					((WrappingLayoutStrategy)spWrapping).SetVirtualizationDirection(Orientation.Horizontal);
				}


				ReevaluateGroupHeaderStrategy();
				// let the base know
				ProcessOrientationChange();
			}
		}
		else if (args.Property == GroupPaddingProperty)
		{
			ILayoutStrategy spWrapping;
			Thickness thickness = (Thickness)args.NewValue;

			spWrapping = GetLayoutStrategy();

			((WrappingLayoutStrategy)spWrapping).SetGroupPadding(thickness);
			InvalidateMeasure();
		}
		else if (args.Property == MaximumRowsOrColumnsProperty)
		{
			ILayoutStrategy spWrapping;
			int maxrowsorcolumns = (int)args.NewValue;
			spWrapping = GetLayoutStrategy();

			((WrappingLayoutStrategy)spWrapping).SetMaximumRowsOrColumns(maxrowsorcolumns);
			InvalidateMeasure();

			// Likely to cause a change in wrapping that could bring in many more containers
			// Reset the cache length to avoid a delay
			ResetCacheBuffers();
		}
		else if (args.Property == ItemWidthProperty)
		{
			ILayoutStrategy spWrapping;
			double itemWidth = (double)args.NewValue;

			spWrapping = GetLayoutStrategy();

			((WrappingLayoutStrategy)spWrapping).SetItemWidth(itemWidth);
			InvalidateMeasure();

			// Likely to cause a change in wrapping that could bring in many more containers
			// Reset the cache length to avoid a delay
			ResetCacheBuffers();
		}
		else if (args.Property == ItemHeightProperty)
		{
			ILayoutStrategy spWrapping;
			double itemHeight = (double)args.NewValue;

			spWrapping = GetLayoutStrategy();

			((WrappingLayoutStrategy)spWrapping).SetItemHeight(itemHeight);
			InvalidateMeasure();

			// Likely to cause a change in wrapping that could bring in many more containers
			// Reset the cache length to avoid a delay
			ResetCacheBuffers();
		}
		else if (args.Property == GroupHeaderPlacementProperty)
		{
			GroupHeaderPlacement placement = (GroupHeaderPlacement)args.NewValue;
			SetGroupHeaderPlacement(placement);
			InvalidateMeasure();
		}
		else if (args.Property == CacheLengthProperty)
		{
			double newCacheLength = (double)args.NewValue;
			CacheLengthBase = newCacheLength;
		}
		else if (args.Property == AreStickyGroupHeadersEnabledProperty)
		{
			// pass the new value to ModernCollectionBasePanel
			AreStickyGroupHeadersEnabledBase = (bool)args.NewValue;
		}
	}

	// Logical Orientation override
	Orientation IOrientedPanel.LogicalOrientation
	{
		get
		{
			Orientation orientation = Orientation.Vertical;

			orientation = Orientation;

			return orientation;
		}
	}

	// Physical Orientation override
	Orientation IOrientedPanel.PhysicalOrientation
	{
		get
		{
			Orientation value = Orientation.Horizontal;
			Orientation orientation = Orientation.Horizontal;

			orientation = Orientation;
			if (orientation == Orientation.Vertical)
			{
				value = Orientation.Horizontal;
			}
			else
			{
				value = Orientation.Vertical;
			}

			return value;
		}
	}

	// Virtual helper method to get the ItemsPerPage that can be overridden by derived classes.
	private protected override double GetItemsPerPageImpl(Rect window)
	{
		double itemsPerPage;
		ILayoutStrategy spWrapping;

		spWrapping = GetLayoutStrategy();
		itemsPerPage = ((WrappingLayoutStrategy)spWrapping).GetItemsPerPage(window);

		return itemsPerPage;
	}

	#region Special elements overrides

	private protected override bool NeedsSpecialItem()
	{
		ILayoutStrategy spStacking;
		bool result = false;

		spStacking = GetLayoutStrategy();
		result = ((WrappingLayoutStrategy)spStacking).NeedsSpecialItem();

		return result;
	}

	private protected override bool NeedsSpecialGroup()
	{
		ILayoutStrategy spStacking;
		bool result = false;

		spStacking = GetLayoutStrategy();
		result = ((WrappingLayoutStrategy)spStacking).NeedsSpecialGroup();

		return result;
	}

	private protected override int GetSpecialItemIndex()
	{
		ILayoutStrategy spStacking;
		int result = -1;

		spStacking = GetLayoutStrategy();
		result = ((WrappingLayoutStrategy)spStacking).GetSpecialItemIndex();

		return result;
	}

	private protected override int GetSpecialGroupIndex()
	{
		ILayoutStrategy spStacking;
		int result = -1;

		spStacking = GetLayoutStrategy();
		result = ((WrappingLayoutStrategy)spStacking).GetSpecialGroupIndex();

		return result;
	}

	private protected override void RegisterSpecialContainerSize(int itemIndex, Size containerDesiredSize)
	{
		ILayoutStrategy spStacking;

		spStacking = GetLayoutStrategy();
		((WrappingLayoutStrategy)spStacking).RegisterSpecialContainerSize(itemIndex, containerDesiredSize);
	}

	private protected override void RegisterSpecialHeaderSize(int groupIndex, Size headerDesiredSize)
	{
		ILayoutStrategy spStacking;

		spStacking = GetLayoutStrategy();
		((WrappingLayoutStrategy)spStacking).RegisterSpecialHeaderSize(groupIndex, headerDesiredSize);
	}

	#endregion

	// Notify the layout strategy of the new header placement.
	private protected override void SetGroupHeaderStrategy(GroupHeaderStrategy strategy)
	{
		ILayoutStrategy spStacking;

		spStacking = GetLayoutStrategy();
		((WrappingLayoutStrategy)spStacking).SetGroupHeaderStrategy(strategy);
	}

	// Invalidates the group cache.
	private protected override void OnCollectionChangeProcessed()
	{
		ILayoutStrategy spWrapping;

		// call base
		base.OnCollectionChangeProcessed();

		spWrapping = GetLayoutStrategy();
		((WrappingLayoutStrategy)spWrapping).InvalidateGroupCache();
	}
}

