using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Extensions;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Private.Infrastructure;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public class Given_RelativePanel
	{
		[TestMethod]
		public async Task When_Padding_Set_In_SizeChanged()
		{
			var SUT = new RelativePanel()
			{
				Width = 200,
				Height = 200,
				Children =
				{
					new Border()
					{
						Child = new Ellipse()
						{
							Fill = new SolidColorBrush(Colors.DarkOrange)
						}
					}
				}
			};

			SUT.SizeChanged += (sender, args) => SUT.Padding = new Thickness(0, 200, 0, 0);

			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForLoaded(SUT);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(200, ((UIElement)VisualTreeHelper.GetChild(SUT, 0)).ActualOffset.Y);
		}

		[TestMethod]
		public async Task When_Child_Aligns_Horizontal_Center_With_Panel()
		{
			var SUT = new RelativePanel()
			{
				Name = "test",
				Width = 300,
				Height = 300
			};
			var border = new RelativePanelMeasuredControl(new Size(100, 100));
			border.SetValue(RelativePanel.AlignHorizontalCenterWithPanelProperty, true);

			SUT.Children.Add(border);
			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(new Rect(100, 0, 100, 100), border.GetRelativeBounds(SUT));
		}

		[TestMethod]
		public async Task When_Child_Aligns_Vertical_Center_With_Panel()
		{
			var SUT = new RelativePanel()
			{
				Name = "test",
				Width = 300,
				Height = 300
			};
			var border = new RelativePanelMeasuredControl(new Size(100, 100));
			border.SetValue(RelativePanel.AlignVerticalCenterWithPanelProperty, true);

			SUT.Children.Add(border);
			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(new Rect(0, 100, 100, 100), border.GetRelativeBounds(SUT));
		}

		[TestMethod]
		public async Task When_Child_Aligns_Two_Directions_Center_With_Panel()
		{
			var SUT = new RelativePanel()
			{
				Name = "test",
				Width = 300,
				Height = 300
			};
			var border = new RelativePanelMeasuredControl(new Size(100, 100));
			border.SetValue(RelativePanel.AlignVerticalCenterWithPanelProperty, true);
			border.SetValue(RelativePanel.AlignHorizontalCenterWithPanelProperty, true);

			SUT.Children.Add(border);
			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(new Rect(100, 100, 100, 100), border.GetRelativeBounds(SUT));
		}

	}

	public partial class RelativePanelMeasuredControl : Control
	{
		private readonly Size _measureSize;

		public RelativePanelMeasuredControl(Size measureSize)
		{
			_measureSize = measureSize;
		}

		protected override Size MeasureOverride(Size availableSize) => _measureSize;
	}
}
