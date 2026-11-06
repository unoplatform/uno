// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//  Abstract:
//      Represents the implementation of our stacking panel.
//      This is only a configuration class. All work actually happens in
//      strategy that is separately defined. We do this for testability
//      and flexibility in layering.

// MUX Reference ItemsStackPanel_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#if LISTVIEW_PORT_CUTOVER

#nullable enable

using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class ItemsStackPanel
{
	public ItemsStackPanel()
	{
		m_itemsUpdatingScrollMode = ItemsUpdatingScrollMode.KeepItemsInView;

		Initialize();
	}

	private void Initialize()
	{
		StackingLayoutStrategy spStacking;

		spStacking = new StackingLayoutStrategy();
		SetLayoutStrategyBase(spStacking);

		// communicate the default
		spStacking.SetVirtualizationDirection(Orientation.Vertical);
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
				ILayoutStrategy spStacking = GetLayoutStrategy();

				CacheFirstVisibleElementBeforeOrientationChange();
				((StackingLayoutStrategy)spStacking).SetVirtualizationDirection(orientation);
				ReevaluateGroupHeaderStrategy();
				// let the base know
				ProcessOrientationChange();
			}
		}
		else if (args.Property == GroupPaddingProperty)
		{
			ILayoutStrategy spStacking;
			Thickness thickness = (Thickness)args.NewValue;

			spStacking = GetLayoutStrategy();

			((StackingLayoutStrategy)spStacking).SetGroupPadding(thickness);
			InvalidateMeasure();
		}
		else if (args.Property == GroupHeaderPlacementProperty)
		{
			GroupHeaderPlacement placement = (GroupHeaderPlacement)args.NewValue;
			SetGroupHeaderPlacement(placement);
			InvalidateMeasure();
		}
		else if (args.Property == ItemsUpdatingScrollModeProperty)
		{
			m_itemsUpdatingScrollMode = (ItemsUpdatingScrollMode)args.NewValue;
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

	#region Special elements overrides

	private protected override bool NeedsSpecialItem()
	{
		ILayoutStrategy spStacking;
		bool result = false;

		spStacking = GetLayoutStrategy();
		result = ((StackingLayoutStrategy)spStacking).NeedsSpecialItem();

		return result;
	}

	private protected override bool NeedsSpecialGroup()
	{
		ILayoutStrategy spStacking;
		bool result = false;

		spStacking = GetLayoutStrategy();
		result = ((StackingLayoutStrategy)spStacking).NeedsSpecialGroup();

		return result;
	}

	private protected override int GetSpecialItemIndex()
	{
		ILayoutStrategy spStacking;
		int result = -1;

		spStacking = GetLayoutStrategy();
		result = ((StackingLayoutStrategy)spStacking).GetSpecialItemIndex();

		return result;
	}

	private protected override int GetSpecialGroupIndex()
	{
		ILayoutStrategy spStacking;
		int result = -1;

		spStacking = GetLayoutStrategy();
		result = ((StackingLayoutStrategy)spStacking).GetSpecialGroupIndex();

		return result;
	}

	private protected override void RegisterSpecialContainerSize(int itemIndex, Size containerDesiredSize)
	{
		ILayoutStrategy spStacking;

		spStacking = GetLayoutStrategy();
		((StackingLayoutStrategy)spStacking).RegisterSpecialContainerSize(itemIndex, containerDesiredSize);
	}

	private protected override void RegisterSpecialHeaderSize(int groupIndex, Size headerDesiredSize)
	{
		ILayoutStrategy spStacking;

		spStacking = GetLayoutStrategy();
		((StackingLayoutStrategy)spStacking).RegisterSpecialHeaderSize(groupIndex, headerDesiredSize);
	}

	#endregion

	// Notify the layout strategy of the new header placement.
	private protected override void SetGroupHeaderStrategy(GroupHeaderStrategy strategy)
	{
		ILayoutStrategy spStacking;

		spStacking = GetLayoutStrategy();
		((StackingLayoutStrategy)spStacking).SetGroupHeaderStrategy(strategy);
	}

	// Returns the size of the potential GroupPadding in the virtualizing direction.
	private protected override double GroupPaddingSizeInVirtualizingDirection()
	{
		double groupPadding = 0.0;

		Thickness thickness = GroupPadding;

		groupPadding = SizeFromThicknessInVirtualizingDirection(thickness);

		return groupPadding;
	}

	// Used to estimate tracked element reposition during items source updates, when a group header is inserted.
	private protected override float GetAverageHeaderSize()
	{
		float averageHeaderSize = -1.0f;

		ILayoutStrategy stackingLayoutStrategy = GetLayoutStrategy();

		averageHeaderSize = ((StackingLayoutStrategy)stackingLayoutStrategy).GetAverageHeaderSize();

		return averageHeaderSize;
	}

	// Used to estimate tracked element reposition during items source updates, when a container is inserted.
	private protected override float GetAverageContainerSize()
	{
		float averageContainerSize = -1.0f;

		ILayoutStrategy stackingLayoutStrategy = GetLayoutStrategy();

		averageContainerSize = ((StackingLayoutStrategy)stackingLayoutStrategy).GetAverageContainerSize();

		return averageContainerSize;
	}

	// Logical Orientation override
	Orientation IOrientedPanel.LogicalOrientation
	{
		get
		{
			Orientation orientation = Orientation.Horizontal;

			orientation = Orientation;

			return orientation;
		}
	}

	// Physical Orientation override
	Orientation IOrientedPanel.PhysicalOrientation => ((IOrientedPanel)this).LogicalOrientation;
}

#endif
