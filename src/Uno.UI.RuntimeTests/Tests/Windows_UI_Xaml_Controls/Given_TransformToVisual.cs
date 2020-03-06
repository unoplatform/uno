using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_TransformToVisual
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task TransformToVisual_WithMargin()
		{
			FrameworkElement inner = new Border {Width = 100, Height = 100, Background = new SolidColorBrush(Colors.DarkBlue)};

			FrameworkElement container = new Border
			{
				Child = inner,
				Margin = new Thickness(1, 3, 5, 7),
				Padding = new Thickness(11, 13, 17, 19),
				BorderThickness = new Thickness(23),
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Bottom,
				Background = new SolidColorBrush(Colors.DarkSalmon)
			};
			FrameworkElement outer = new Border
			{
				Child = container,
				Padding = new Thickness(8),
				BorderThickness = new Thickness(2),
				Width = 300,
				Height = 300,
				Background = new SolidColorBrush(Colors.MediumSeaGreen)
			};

			TestServices.WindowHelper.WindowContent = outer;

			await TestServices.WindowHelper.WaitForIdle();

			string GetStr(FrameworkElement e)
			{
				var positionMatrix = ((MatrixTransform)e.TransformToVisual(outer)).Matrix;
				return $"{positionMatrix.OffsetX};{positionMatrix.OffsetY};{e.ActualWidth};{e.ActualHeight}";
			}

			var str = $"{GetStr(container)}|{GetStr(inner)}";
			Assert.AreEqual("111;105;174;178|145;141;100;100", str);
		}
	}
}
