using System.Drawing;
using System.Linq;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;

namespace SamplesApp.UITests.Windows_UI_Xaml_Shapes
{
	public partial class Shapes_Tests
	{
		private const string _clearDataSample = "UITests.Windows_UI_Xaml_Shapes.Path_ClearData";

		private static readonly ScreenshotOptions _opts = new ScreenshotOptions {IgnoreInSnapshotCompare = true};

		[Test]
		[AutoRetry]
		public void Path_ClearData_When_Control()
		{
			Run(_clearDataSample, skipInitialScreenshot: true);

			var sut = _app.WaitForElement("ControlDataParent").Single().Rect;
			var result = TakeScreenshot("result", _opts);

			ImageAssert.HasColorAt(result, sut.CenterX, sut.CenterY, Color.Green);
		}

		[Test]
		[AutoRetry]
		public void Path_ClearData_When_ClearData()
		{
			Run(_clearDataSample);

			var sut = _app.WaitForElement("ClearedDataParent").Single().Rect;
			var result = TakeScreenshot("result", _opts);

			ImageAssert.HasColorAt(result, sut.CenterX, sut.CenterY, Color.White);
		}

		[Test]
		[AutoRetry]
		public void Path_ClearData_When_NoData()
		{
			Run(_clearDataSample);

			var sut = _app.WaitForElement("NoDataShape").Single().Rect;
			var result = TakeScreenshot("result", _opts);

			ImageAssert.HasColorAt(result, sut.CenterX, sut.CenterY, Color.White);
		}
	}
}
