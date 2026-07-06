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

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_RaiseItemsInfoRequested_AndHandlerProvidesInfo_Then_ReturnsHandlerValues()
	{
		var sut = new LinedFlowLayout();
		LinedFlowLayoutItemsInfoRequestedEventArgs capturedArgs = null;

		sut.ItemsInfoRequested += (s, e) =>
		{
			capturedArgs = e;
			e.SetDesiredAspectRatios(new[] { 1.0, 2.0, 0.5 });
			e.MinWidth = 10;
			e.MaxWidth = 100;
		};

		var info = sut.RaiseItemsInfoRequested(0, 3);

		// The handler must be invoked with the requested range.
		capturedArgs.Should().NotBeNull("the ItemsInfoRequested handler must be invoked");
		capturedArgs.ItemsRangeStartIndex.Should().Be(0);
		capturedArgs.ItemsRangeRequestedLength.Should().Be(3);

		// The returned ItemsInfo must reflect the sizing info the handler provided.
		info.m_itemsRangeStartIndex.Should().Be(0);
		info.m_itemsRangeLength.Should().Be(3);
		info.m_minWidth.Should().Be(10.0);
		info.m_maxWidth.Should().Be(100.0);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_RaiseItemsInfoRequested_AndNoHandler_Then_ReturnsEmpty()
	{
		var sut = new LinedFlowLayout();

		// No ItemsInfoRequested subscriber -> the empty items info (all -1) is returned.
		var info = sut.RaiseItemsInfoRequested(0, 3);

		info.m_itemsRangeStartIndex.Should().Be(-1);
		info.m_itemsRangeLength.Should().Be(-1);
		info.m_minWidth.Should().Be(-1.0);
		info.m_maxWidth.Should().Be(-1.0);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_RaiseItemsInfoRequested_AndHandlerLowersStartIndex_Then_ReturnsExpandedRange()
	{
		var sut = new LinedFlowLayout();

		sut.ItemsInfoRequested += (s, e) =>
		{
			// A handler may provide MORE info than requested by lowering the start index (never raising it),
			// then supplying an array that covers the enlarged range.
			e.ItemsRangeStartIndex = 0;
			e.SetDesiredAspectRatios(new[] { 1.0, 1.0, 1.0, 1.0, 1.0 });
		};

		var info = sut.RaiseItemsInfoRequested(2, 3);

		info.m_itemsRangeStartIndex.Should().Be(0);
		info.m_itemsRangeLength.Should().Be(5);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_RequestedRange_AndNoItemsInfo_Then_RegularPathEmpty()
	{
		var sut = new LinedFlowLayout();

		// Fresh instance: no arrange-width info was provided -> the regular path is used,
		// with first index -1 and length 0.
		sut.RequestedRangeStartIndex.Should().Be(-1);
		sut.RequestedRangeLength.Should().Be(0);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_GetArrangeWidth_Then_ClampsAndScales()
	{
		var sut = new LinedFlowLayout();

		// No min/max (min < 0 -> treated as 0; max < 0 -> no upper clamp), scale 1: width = ratio * height.
		sut.GetArrangeWidth(2.0, -1.0, -1.0, 50.0, 1.0).Should().Be(100.0);

		// Min clamp: 1.0 * 50 = 50 is floored up to minWidth 80.
		sut.GetArrangeWidth(1.0, 80.0, -1.0, 50.0, 1.0).Should().Be(80.0);

		// Max clamp: 3.0 * 50 = 150 is capped to maxWidth 100.
		sut.GetArrangeWidth(3.0, -1.0, 100.0, 50.0, 1.0).Should().Be(100.0);

		// Scale down (< 1): 2.0 * 50 = 100, * 0.5 = 50, re-floored to minWidth (0) -> 50.
		sut.GetArrangeWidth(2.0, -1.0, -1.0, 50.0, 0.5).Should().Be(50.0);

		// Scale down but re-floored to minWidth: 100 * 0.5 = 50, floored up to minWidth 80.
		sut.GetArrangeWidth(2.0, 80.0, -1.0, 50.0, 0.5).Should().Be(80.0);

		// Scale up (> 1) but re-capped to maxWidth: 1.0 * 50 = 50 (<= max 60), * 2 = 100, capped to 60.
		sut.GetArrangeWidth(1.0, -1.0, 60.0, 50.0, 2.0).Should().Be(60.0);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_GetDesiredAspectRatioFromItemsInfo_FastPath_Then_ReadsSeededBuffer()
	{
		var sut = new LinedFlowLayout();

		// The ItemsInfoRequested handler's SetDesiredAspectRatios seam populates the fast-path buffer.
		sut.SetDesiredAspectRatios(new[] { 1.5, 2.0, 0.5 });

		sut.GetDesiredAspectRatioFromItemsInfo(0, usesFastPathLayout: true).Should().Be(1.5);
		sut.GetDesiredAspectRatioFromItemsInfo(1, usesFastPathLayout: true).Should().Be(2.0);
		sut.GetDesiredAspectRatioFromItemsInfo(2, usesFastPathLayout: true).Should().Be(0.5);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_GetMinMaxWidthFromItemsInfo_AndNoInfo_Then_ReturnsGlobalDefault()
	{
		var sut = new LinedFlowLayout();

		// Fresh instance: no per-item widths and no global min/max -> the -1.0 sentinel is returned
		// without indexing the empty regular-path buffers (m_itemsInfoFirstIndex == -1, so the
		// itemIndex - m_itemsInfoFirstIndex bounds check must guard against the empty list).
		sut.GetMinWidthFromItemsInfo(0).Should().Be(-1.0);
		sut.GetMaxWidthFromItemsInfo(0).Should().Be(-1.0);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_GetAverageAspectRatio_AndNoStorage_Then_ReturnsDefault()
	{
		var sut = new LinedFlowLayout();

		// No aspect-ratio storage and average-items-per-line still 0 -> default aspect ratio 1.0.
		sut.GetAverageAspectRatio(800f, 50.0).Should().Be(1.0);

		// A zero line height also short-circuits to the default aspect ratio 1.0.
		sut.GetAverageAspectRatio(800f, 0.0).Should().Be(1.0);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_AverageItemsPerLineSet_Then_GetAverageAspectRatioUsesIt()
	{
		var sut = new LinedFlowLayout();

		// Seed the snapped average-items-per-line; with no storage GetAverageAspectRatio derives the
		// average ratio from availableWidth / averageItemsPerLine.second / actualLineHeight.
		sut.SetAverageItemsPerLine((first: 4.0, second: 4.0), unlockItems: false);

		// 800 / 4.0 / 50 = 4.0
		sut.GetAverageAspectRatio(800f, 50.0).Should().Be(4.0);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_GetLinesDesiredWidth_AndNoFrozenLines_Then_ReturnsZero()
	{
		var sut = new LinedFlowLayout();

		// Fresh instance: no frozen lines computed yet (m_firstFrozenLineIndex == -1).
		sut.GetLinesDesiredWidth().Should().Be(0.0f);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_GetLineIndex_ForFirstItem_Then_ReturnsZero()
	{
		var sut = new LinedFlowLayout();

		// Item 0 short-circuits to line 0 (may occur while the average items per line is still 0).
		sut.GetLineIndex(0, usesFastPathLayout: false).Should().Be(0);
		sut.GetLineIndex(0, usesFastPathLayout: true).Should().Be(0);
	}
}
