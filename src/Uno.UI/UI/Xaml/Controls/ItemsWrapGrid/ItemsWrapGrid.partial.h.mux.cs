// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ItemsWrapGrid_Partial.h, tag winui3/release/2.5.1, commit ba3a8d59e6d


#nullable enable

using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Controls;

public partial class ItemsWrapGrid : ModernCollectionBasePanel, IOrientedPanel
{
	// Properties missing from the mixin-generated ones (Orientation, GroupPadding, GroupHeaderPlacement, AreStickyGroupHeadersEnabled, ItemWidth, ItemHeight, MaximumRowsOrColumns).
	public static DependencyProperty CacheLengthProperty { get; } =
		DependencyProperty.Register(
			nameof(CacheLength),
			typeof(double),
			typeof(ItemsWrapGrid),
			new FrameworkPropertyMetadata(Boxer.Box(GetDefaultCacheLength())));

	public double CacheLength
	{
		get => (double)GetValue(CacheLengthProperty);
		set => SetValue(CacheLengthProperty, value);
	}

	// a wrapgrid is able to portal (AddDeleteTransition)
	// TODO Uno: there is no portalling transition base on Uno, so nothing reads this yet.
	internal bool IsPortallingSupported() => true;

	internal override Orientation? PhysicalOrientation => Orientation;

	public int FirstCacheIndex => FirstCacheIndexBase;
	public int FirstVisibleIndex => FirstVisibleIndexBase;
	public int LastVisibleIndex => LastVisibleIndexBase;
	public int LastCacheIndex => LastCacheIndexBase;
	public PanelScrollingDirection ScrollingDirection => PanningDirectionBase;
}

