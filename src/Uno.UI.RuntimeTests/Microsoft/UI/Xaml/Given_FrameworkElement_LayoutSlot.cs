using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Helpers;
using Private.Infrastructure;
using Windows.Foundation;
using Microsoft.UI;
using Windows.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

#if HAS_UNO && !HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_FrameworkElement_LayoutSlot
	{
		[TestMethod]
		[DataRow(67, 29)] // centring offset 28.5: banker's rounding would give 28
		[DataRow(71, 31)] // centring offset 30.5: banker's rounding would give 30
		public async Task When_Centered_At_Half_Pixel_Then_Rounds_Half_Up(double containerSize, double expectedOffset)
		{
			var SUT = new Border { Width = 10, Height = 10, HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center, VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center };
			var container = new Grid { Width = containerSize, Height = containerSize, Children = { SUT } };

			await UITestHelper.Load(container);

			if (container.XamlRoot.RasterizationScale != 1)
			{
				Assert.Inconclusive("The half-pixel offsets assume a rasterization scale of 1.");
			}

			var offset = SUT.TransformToVisual(container).TransformPoint(default);
			Assert.AreEqual(new Point(expectedOffset, expectedOffset), offset);
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task When_Border_Applied_In_Templated_Control()
		{
			var SUT = new Border()
			{
				Background = new SolidColorBrush(Colors.Red),
				Width = 100,
				Height = 100
			};

			var button = new Button()
			{
				BorderBrush = new SolidColorBrush(Colors.Blue),
				BorderThickness = new Microsoft.UI.Xaml.Thickness(50),
				Content = SUT,
				Padding = new Microsoft.UI.Xaml.Thickness(0),
				Margin = new Microsoft.UI.Xaml.Thickness(0),
				VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Top
			};

			TestServices.WindowHelper.WindowContent = button;
			await TestServices.WindowHelper.WaitForLoaded(button);

			var transform = SUT.TransformToVisual(null);
			var point = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
			Assert.AreEqual(new Point(50, 50), point);
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task When_Border_Applied_In_Templated_Control_And_Page()
		{
			var SUT = new Border()
			{
				Background = new SolidColorBrush(Colors.Red),
				Width = 100,
				Height = 100
			};

			var button = new Button()
			{
				BorderBrush = new SolidColorBrush(Colors.Blue),
				BorderThickness = new Microsoft.UI.Xaml.Thickness(50),
				Content = SUT,
				Padding = new Microsoft.UI.Xaml.Thickness(0),
				Margin = new Microsoft.UI.Xaml.Thickness(0),
				VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Top
			};

			var page = new Page()
			{
				BorderBrush = new SolidColorBrush(Colors.Pink),
				BorderThickness = new Microsoft.UI.Xaml.Thickness(50),
				Content = button,
			};

			TestServices.WindowHelper.WindowContent = page;
			await TestServices.WindowHelper.WaitForLoaded(page);

			var transform = SUT.TransformToVisual(null);
			var point = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
			Assert.AreEqual(new Point(50, 50), point);
		}
	}
}
