#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

// Not a port of WinUI's OrientedVirtualizingPanel-based CarouselPanel, which needs IScrollInfo and a working
// ItemContainerGenerator. Uno borrows the ItemsStackPanel stacking strategy instead, and does not carousel.
// TODO Uno: port the real OrientedVirtualizingPanel-based CarouselPanel, see #26199.
public partial class CarouselPanel : ModernCollectionBasePanel, IOrientedPanel
{
	public CarouselPanel()
	{
		StackingLayoutStrategy strategy = new();
		SetLayoutStrategyBase(strategy);
		strategy.SetVirtualizationDirection(Orientation.Vertical);

		// Small, to somewhat resemble the UWP CarouselPanel, which doesn't expose a configurable CacheLength.
		CacheLengthBase = 0.5;
	}

	private StackingLayoutStrategy Strategy => (StackingLayoutStrategy)GetLayoutStrategy();

	private protected override bool NeedsSpecialItem() => Strategy.NeedsSpecialItem();

	private protected override bool NeedsSpecialGroup() => Strategy.NeedsSpecialGroup();

	private protected override int GetSpecialItemIndex() => Strategy.GetSpecialItemIndex();

	private protected override int GetSpecialGroupIndex() => Strategy.GetSpecialGroupIndex();

	private protected override void RegisterSpecialContainerSize(int itemIndex, Size containerDesiredSize)
		=> Strategy.RegisterSpecialContainerSize(itemIndex, containerDesiredSize);

	private protected override void RegisterSpecialHeaderSize(int groupIndex, Size headerDesiredSize)
		=> Strategy.RegisterSpecialHeaderSize(groupIndex, headerDesiredSize);

	private protected override void SetGroupHeaderStrategy(GroupHeaderStrategy strategy)
		=> Strategy.SetGroupHeaderStrategy(strategy);

	private protected override float GetAverageHeaderSize() => Strategy.GetAverageHeaderSize();

	private protected override float GetAverageContainerSize() => Strategy.GetAverageContainerSize();

	Orientation IOrientedPanel.LogicalOrientation => Orientation.Vertical;

	Orientation IOrientedPanel.PhysicalOrientation => Orientation.Vertical;

	internal override Orientation? PhysicalOrientation => Orientation.Vertical;

	public new bool AreHorizontalSnapPointsRegular => base.AreHorizontalSnapPointsRegular;

	public new bool AreVerticalSnapPointsRegular => base.AreVerticalSnapPointsRegular;

	public new IReadOnlyList<float> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment alignment)
		=> base.GetIrregularSnapPoints(orientation, alignment);

	public float GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment alignment, out float offset)
	{
		base.GetRegularSnapPoints(orientation, alignment, out offset, out var spacing);
		return spacing;
	}

	public new event EventHandler<object> HorizontalSnapPointsChanged
	{
		add => base.HorizontalSnapPointsChanged += value;
		remove => base.HorizontalSnapPointsChanged -= value;
	}

	public new event EventHandler<object> VerticalSnapPointsChanged
	{
		add => base.VerticalSnapPointsChanged += value;
		remove => base.VerticalSnapPointsChanged -= value;
	}
}
