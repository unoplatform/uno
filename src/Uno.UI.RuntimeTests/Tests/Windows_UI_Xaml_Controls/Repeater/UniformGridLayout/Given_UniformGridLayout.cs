using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
#if HAS_UNO && !HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
#endif
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Repeater;

[TestClass]
public class Given_UniformGridLayout
{
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_AvailableMinorSizeSmallerThanItem_Then_DoesNotThrow()
	{
		// Regression test for https://github.com/unoplatform/uno/issues/23366
		// When a UniformGridLayout is measured with a finite minor-axis (here: width) available size
		// that is smaller than a single MinItemWidth, GetItemsPerLine used to truncate the items-per-line
		// count to 0. GetMajorSize / GetLayoutRectForDataIndex then divided by that count, throwing an
		// unhandled DivideByZeroException that took down the dispatcher loop.
		// The layout must instead clamp to a single column, matching WinUI.
		using var template = new DynamicDataTemplate(() => new Border
		{
			Background = new SolidColorBrush(Microsoft.UI.Colors.SkyBlue),
			Child = new TextBlock()
		});
		var sut = new ItemsRepeater
		{
			ItemsSource = Enumerable.Range(0, 20),
			ItemTemplate = template.Value,
			Layout = new UniformGridLayout
			{
				MinItemWidth = 140,
				MinItemHeight = 40,
			}
		};

		// The host is deliberately narrower than a single MinItemWidth (140) so that the
		// minor-axis (width) available size passed to the layout is smaller than one item.
		var host = new Grid
		{
			Width = 100,
			Height = 300,
			Children = { sut }
		};

		// Loading and laying out used to throw a DivideByZeroException through the layout pass.
		await UITestHelper.Load(host, h => h.ActualWidth > 0 && h.ActualHeight > 0);

		// Force extra measure passes through the realization-rect anchoring path where the
		// exception used to surface, to make the repro deterministic.
		sut.InvalidateMeasure();
		host.UpdateLayout();
		await UITestHelper.WaitForIdle();

		// Reaching this point means the layout clamped to a single column instead of throwing.
		// The first item must have been realized.
		sut.TryGetElement(0).Should().NotBeNull("the layout should clamp to a single column instead of throwing");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_VerticalOrientation_Fill_Then_ItemMeasuredWithStretchedHeight()
	{
		// Regression test for the vertical-orientation branch of UniformGridLayoutState.CalculateAvailableSize,
		// which used to measure items with the raw MinItemHeight instead of the stretch-adjusted row height.
		// With ItemsStretch.Fill the available minor (height) space is distributed across the items, so an
		// item whose width depends on the height it is measured with must observe the stretched height.
		// Before the fix the item was measured with the un-stretched MinItemHeight, producing a too-small width.
		using var template = new DynamicDataTemplate(() => new AspectRatioControl());
		var sut = new ItemsRepeater
		{
			ItemsSource = Enumerable.Range(0, 20),
			ItemTemplate = template.Value,
			Layout = new UniformGridLayout
			{
				Orientation = Orientation.Vertical,
				ItemsStretch = UniformGridLayoutItemsStretch.Fill,
				MinItemHeight = 44,
				// MinItemWidth intentionally left unset (NaN) so the effective width is taken from the measured item.
			}
		};

		var host = new Grid
		{
			Width = 400,
			Height = 300,
			Children = { sut }
		};

		await UITestHelper.Load(host, h => h.ActualWidth > 0 && h.ActualHeight > 0);
		await UITestHelper.WaitForIdle();

		// A 300px column with MinItemHeight = 44 fits 6 items (264px), leaving 36px that Fill distributes as
		// 6px per item, so each item's row height is stretched to 50. AspectRatioControl reports
		// width = 2 * measured-height, so when the item is measured with the stretched row height its desired
		// width is 100. Before the fix CalculateAvailableSize returned the raw MinItemHeight (44) for the
		// vertical branch, so the item was measured with 44 and reported a desired width of only 88.
		// (Verified fails-before/passes-after: DesiredSize.Width is 88 without the fix, 100 with it.)
		var item = sut.TryGetElement(0) as FrameworkElement;
		item.Should().NotBeNull();
		((double)item!.DesiredSize.Width).Should().BeGreaterThan(94,
			"the item must be measured with the stretch-adjusted row height (50), not the raw MinItemHeight (44)");
	}

	private partial class AspectRatioControl : FrameworkElement
	{
		protected override Size MeasureOverride(Size availableSize)
		{
			// Width tracks the height this control is measured with (2:1), so the width observed after layout
			// reveals which height the UniformGridLayout used when measuring the item.
			var height = double.IsInfinity(availableSize.Height) ? 44 : availableSize.Height;
			return new Size(height * 2, height);
		}
	}
}
