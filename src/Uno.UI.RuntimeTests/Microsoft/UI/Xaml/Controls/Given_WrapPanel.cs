using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Windows.UI;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_WrapPanel
	{
#if HAS_UNO
		[TestMethod]
		[RequiresScaling(1.25f)]
		public async Task When_WrapPanel_NotWrap_WhenFit()
		{
			// Total content width: 44 (Child0) + 11 (AppBarSeparator w/ Margin=5,0) + 44 (Child2) + 44 (Child3) = 143px
			// StackPanel MinWidth=200, WrapPanel Margin=2 → available width ≥ 196px > 143px → no wrap expected
			var child0 = new Border { Width = 44, Height = 20, Background = new SolidColorBrush(Colors.Red) };
			var child1 = new AppBarSeparator { Margin = new Thickness(5, 0, 5, 0) };
			var child2 = new Border { Width = 44, Height = 20, Background = new SolidColorBrush(Colors.Green) };
			var child3 = new Border { Width = 44, Height = 20, Background = new SolidColorBrush(Colors.Blue) };

			var wrapPanel = new WrapPanel
			{
				Margin = new Thickness(2),
				Background = new SolidColorBrush(Colors.Pink),
				HorizontalAlignment = HorizontalAlignment.Center,
				Orientation = Orientation.Horizontal,
			};
			wrapPanel.Children.Add(child0);
			wrapPanel.Children.Add(child1);
			wrapPanel.Children.Add(child2);
			wrapPanel.Children.Add(child3);

			var root = new StackPanel { MinWidth = 200 };
			root.Children.Add(wrapPanel);

			WindowHelper.WindowContent = root;
			await WindowHelper.WaitForLoaded(wrapPanel);
			await WindowHelper.WaitForIdle();

			// Children on the same row share the same Y offset.
			// If child3 wraps to row 2, its Y offset would be ~20px (the row height).
			Assert.AreEqual(
				child0.ActualOffset.Y,
				child3.ActualOffset.Y,
				"Child3_SUT must not be wrapped to a second row. (note: test cleanup will reset RasterizationScale, dont trust screenshot.)");
		}
#endif
	}
}
