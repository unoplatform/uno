using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Repeater;

[TestClass]
public class Given_LinedFlowLayout
{
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Constructed_Then_DefaultsMatchWinUI()
	{
		var sut = new LinedFlowLayout();

		double.IsNaN(sut.LineHeight).Should().BeTrue("LineHeight defaults to NaN in WinUI");
		sut.ActualLineHeight.Should().Be(0.0);
		sut.LineSpacing.Should().Be(0.0);
		sut.MinItemSpacing.Should().Be(0.0);
		sut.ItemsJustification.Should().Be(LinedFlowLayoutItemsJustification.Start);
		sut.ItemsStretch.Should().Be(LinedFlowLayoutItemsStretch.None);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_PropertiesSet_Then_RoundTrip()
	{
		var sut = new LinedFlowLayout
		{
			LineHeight = 48,
			LineSpacing = 4,
			MinItemSpacing = 6,
			ItemsJustification = LinedFlowLayoutItemsJustification.SpaceEvenly,
			ItemsStretch = LinedFlowLayoutItemsStretch.Fill,
		};

		sut.LineHeight.Should().Be(48);
		sut.LineSpacing.Should().Be(4);
		sut.MinItemSpacing.Should().Be(6);
		sut.ItemsJustification.Should().Be(LinedFlowLayoutItemsJustification.SpaceEvenly);
		sut.ItemsStretch.Should().Be(LinedFlowLayoutItemsStretch.Fill);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_SnapToPower_Then_SnapsToNearestPowerOf1_1()
	{
		var sut = new LinedFlowLayout();

		// WinUI doc example: value 3.75 snaps to 1.1^14 = 3.7975 (ceil is closest).
		sut.SnapToPower(3.75, 1.1).Should().BeApproximately(3.7975, 0.0005);
		// value 4.00 snaps up to 1.1^15 = 4.1772 (ceil is closest).
		sut.SnapToPower(4.00, 1.1).Should().BeApproximately(4.1772, 0.0005);
		// An exact power snaps to itself.
		sut.SnapToPower(Math.Pow(1.1, 10), 1.1).Should().BeApproximately(Math.Pow(1.1, 10), 0.0005);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_GetLineIndexFromAverageItemsPerLine_Then_TruncatesTowardZero()
	{
		var sut = new LinedFlowLayout();

		sut.GetLineIndexFromAverageItemsPerLine(0, 2.0).Should().Be(0);
		sut.GetLineIndexFromAverageItemsPerLine(5, 2.5).Should().Be(2); // 5 / 2.5 = 2.0
		sut.GetLineIndexFromAverageItemsPerLine(10, 3.0).Should().Be(3); // 10 / 3 = 3.33 -> 3
		sut.GetLineIndexFromAverageItemsPerLine(9, 3.0).Should().Be(3); // 9 / 3 = 3.0
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_GetLineCount_AndCollectionEmpty_Then_Zero()
	{
		var sut = new LinedFlowLayout();

		// No context attached yet -> item count is 0 -> no lines.
		sut.GetLineCount(3.0).Should().Be(0);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_SnapAverageItemsPerLine_NearMidpoint_Then_RetainsOldSnappedValue()
	{
		var sut = new LinedFlowLayout();

		// WinUI doc example: old raw 3.95 (snapped 1.1^14 = 3.7975), new raw 4.00 (would snap to
		// 1.1^15 = 4.1772). Because |3.95 - 4.00| <= 0.1, the old pair is retained for stability.
		var (first, second) = sut.SnapAverageItemsPerLine(3.95, 4.00);
		first.Should().Be(3.95);
		second.Should().BeApproximately(3.7975, 0.0005);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_SnapAverageItemsPerLine_FarApart_Then_UsesNewSnappedValue()
	{
		var sut = new LinedFlowLayout();

		// Raw values far apart (|0 - 5| > 0.1) -> the new pair is used.
		var (first, second) = sut.SnapAverageItemsPerLine(0.0, 5.0);
		first.Should().Be(5.0);
		second.Should().BeApproximately(sut.SnapToPower(5.0, 1.1), 0.0005);
	}
}
