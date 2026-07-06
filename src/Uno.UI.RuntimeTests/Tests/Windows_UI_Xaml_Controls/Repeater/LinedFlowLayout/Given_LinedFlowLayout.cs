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
}
