using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Shapes;

[TestClass]
public class Given_ShapesMeasure_UITest
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Collapsed_Shape_In_Small_ScrollViewer()
	{
		var content = new Grid
		{
			Width = 100,
			Height = 100,
			Children =
			{
				new Ellipse { Fill = new SolidColorBrush(Colors.DeepPink), Visibility = Visibility.Collapsed },
				new TextBlock { Text = "This must not be scrollable" },
			}
		};

		var sut = new ScrollViewer
		{
			Width = 100,
			Height = 100,
			HorizontalAlignment = HorizontalAlignment.Left,
			Background = new SolidColorBrush(Colors.DeepSkyBlue),
			Content = content,
		};

		try
		{
			await UITestHelper.Load(sut);

			// A Collapsed shape must not be measured, so it must not inflate the scrollable extent.
			Assert.AreEqual(0d, sut.ScrollableWidth, "ScrollViewer should not be horizontally scrollable");
			Assert.AreEqual(0d, sut.ScrollableHeight, "ScrollViewer should not be vertically scrollable");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
