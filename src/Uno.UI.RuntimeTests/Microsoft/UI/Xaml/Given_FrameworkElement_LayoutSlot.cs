using System.Threading.Tasks;
using Private.Infrastructure;
using Windows.Foundation;
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
