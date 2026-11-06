// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ItemsStackPanel_Partial.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

#if LISTVIEW_PORT_CUTOVER

#nullable enable

using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Controls;

public partial class ItemsStackPanel : ModernCollectionBasePanel, IOrientedPanel
{
	// Properties missing from the mixin-generated ones (Orientation, GroupPadding, GroupHeaderPlacement, AreStickyGroupHeadersEnabled).
	public static DependencyProperty CacheLengthProperty { get; } =
		DependencyProperty.Register(
			nameof(CacheLength),
			typeof(double),
			typeof(ItemsStackPanel),
			new FrameworkPropertyMetadata(Boxer.Box(GetDefaultCacheLength())));

	public double CacheLength
	{
		get => (double)GetValue(CacheLengthProperty);
		set => SetValue(CacheLengthProperty, value);
	}

	public static DependencyProperty ItemsUpdatingScrollModeProperty { get; } =
		DependencyProperty.Register(
			nameof(ItemsUpdatingScrollMode),
			typeof(ItemsUpdatingScrollMode),
			typeof(ItemsStackPanel),
			new FrameworkPropertyMetadata(ItemsUpdatingScrollMode.KeepItemsInView));

	public ItemsUpdatingScrollMode ItemsUpdatingScrollMode
	{
		get => (ItemsUpdatingScrollMode)GetValue(ItemsUpdatingScrollModeProperty);
		set => SetValue(ItemsUpdatingScrollModeProperty, value);
	}

	// Only ItemsStackPanel and CalendarPanel support the maintain viewport behavior.
	private protected override ItemsUpdatingScrollMode GetItemsUpdatingScrollMode() => m_itemsUpdatingScrollMode;

	public int FirstCacheIndex => FirstCacheIndexBase;
	public int FirstVisibleIndex => FirstVisibleIndexBase;
	public int LastVisibleIndex => LastVisibleIndexBase;
	public int LastCacheIndex => LastCacheIndexBase;
	public PanelScrollingDirection ScrollingDirection => PanningDirectionBase;

	private ItemsUpdatingScrollMode m_itemsUpdatingScrollMode;
}

#endif
