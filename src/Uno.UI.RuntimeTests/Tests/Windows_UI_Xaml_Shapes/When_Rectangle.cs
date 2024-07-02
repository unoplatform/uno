#if __IOS__ || __MACOS__ || __SKIA__
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.Extensions;
using Windows.Devices.Perception;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Shapes
{
	[TestClass]
	public class When_Rectangle
	{
		[TestMethod]
		[RunsOnUIThread]
		[DataRow(Stretch.Fill, double.NaN, double.NaN, 0d, 0d, 50d, 50d, 0d, 0d)]
		[DataRow(Stretch.Fill, double.NaN, double.NaN, 10d, 20d, 50d, 50d, 10d, 20d)]
		[DataRow(Stretch.Fill, 30d, double.NaN, 10d, 20d, 50d, 50d, 30d, 20d)]
		[DataRow(Stretch.Fill, double.NaN, 30d, 10d, 20d, 50d, 50d, 10d, 30d)]
		[DataRow(Stretch.UniformToFill, double.NaN, double.NaN, 0d, 0d, double.PositiveInfinity, double.PositiveInfinity, 0d, 0d)]
		[DataRow(Stretch.UniformToFill, double.NaN, double.NaN, 10d, 20d, double.PositiveInfinity, double.PositiveInfinity, 10d, 20d)]
		public async Task When_RectangleMeasure(
			Stretch stretch,
			double width,
			double height,
			double minWidth,
			double minHeight,
			double availableWidth,
			double availableHeight,
			double expectedWidth,
			double expectedHeight)
		{
			try
			{
				var SUT = new Rectangle();

				TestServices.WindowHelper.WindowContent = SUT;
				await TestServices.WindowHelper.WaitForIdle();

				SUT.Width = width;
				SUT.Height = height;
				SUT.MinWidth = minWidth;
				SUT.MinHeight = minHeight;
				SUT.Stretch = stretch;
				SUT.Measure(new Windows.Foundation.Size(availableWidth, availableHeight));

				Assert.AreEqual(new Size(expectedWidth, expectedHeight), SUT.DesiredSize);
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}
	}
}
#endif
