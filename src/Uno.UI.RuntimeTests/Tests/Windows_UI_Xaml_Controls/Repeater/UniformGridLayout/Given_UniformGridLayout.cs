using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using FluentAssertions;
#if HAS_UNO && !HAS_UNO_WINUI
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
#endif
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Repeater;

[TestClass]
public class Given_UniformGridLayout
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_AdaptiveChildren_Then_DoesNotConstraintsThem()
	{
		using var template = new DynamicDataTemplate(() => new TextBlock
		{
			Text = string.Join(' ', Enumerable.Range(0, 16).Select(i => $"some_text_{i:D2}")), // A dynamically sizable item
			TextWrapping = TextWrapping.Wrap
		});
		var sut = new ItemsRepeater
		{
			Width = 400,
			ItemsSource = Enumerable.Range(0, 10),
			ItemTemplate = template.Value,
			Layout = new UniformGridLayout
			{
				ItemsStretch = UniformGridLayoutItemsStretch.Fill,
				MaximumRowsOrColumns = 2,
				MinColumnSpacing = 10,
				MinItemWidth = 75,
				MinRowSpacing = 10,
			}
		};

		await UITestHelper.Load(sut);

		var firstItem = VisualTreeHelper.GetChild(sut, 0) as UIElement;
		var originalHeight = firstItem!.ActualSize.Y;

		// Force a resize just to validate that size is coherent
		sut.Width = 200;
		sut.UpdateLayout();
		await TestServices.WindowHelper.WaitForIdle();

		var newHeight = firstItem.ActualSize.Y;

		originalHeight.Should().BeGreaterThan(50);
		newHeight.Should().BeGreaterThan(originalHeight);

		Math.Abs(originalHeight * 2 - newHeight).Should().BeLessThan(20, "we devided the width by 2, so the height of each should be about the double of the original"); // We allow about a line of difference for wrapping consideration
	}
}
