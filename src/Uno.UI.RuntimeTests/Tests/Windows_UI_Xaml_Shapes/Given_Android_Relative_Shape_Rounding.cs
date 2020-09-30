#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Shapes
{
	[TestClass]
	public class Given_Android_Relative_Shape_Rounding
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Rectangle_Rounded_Measure_Height()
		{
			var grid = new Grid()
			{
				Height = 439,
			};

			TestServices.WindowHelper.WindowContent = grid;
			await TestServices.WindowHelper.WaitForIdle();

			grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
			grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
			grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

			var topBorder = new Border();
			Grid.SetRow(topBorder, 0);

			var rect = new Rectangle();
			Grid.SetRow(rect, 1);

			var bottomBorder = new Border();
			Grid.SetRow(bottomBorder, 2);

			grid.Children.Add(topBorder);
			grid.Children.Add(rect);
			grid.Children.Add(bottomBorder);

			grid.Measure(new Size(10000, 10000));
			var desired = grid.DesiredSize;
			grid.Arrange(new Rect(0, 0, desired.Width, desired.Height));

			var nativeViewTopBorder = topBorder as Android.Views.View;
			var nativeViewRect = rect as Android.Views.View;
			var nativeViewBottomBorder = bottomBorder as Android.Views.View;

			Assert.AreEqual(146, nativeViewTopBorder.Height);
			Assert.AreEqual(147, nativeViewRect.Height);
			Assert.AreEqual(146, nativeViewBottomBorder.Height);

			Assert.AreEqual(146, nativeViewRect.Top);
			Assert.AreEqual(293, nativeViewRect.Bottom);
			Assert.AreEqual(293, nativeViewBottomBorder.Top);

			Assert.IsNotNull(rect.FrameRoundingAdjustment);
			Assert.AreEqual(1, rect.FrameRoundingAdjustment.Value.Height);
			Assert.AreEqual(0, rect.FrameRoundingAdjustment.Value.Width);
		}
	}
}
#endif
