using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Shapes
{
	public class Mesaure_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void Measure_CollapsedShapeInSmallScrollViewer()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Shapes.MeasurePage");

			var sut = _app.WaitForElement("CollapsedInSmallScrollViewer").Single();
			var initial = TakeScreenshot("initial");

			// Try to scroll up
			_app.DragCoordinates(sut.Rect.CenterX, sut.Rect.Bottom - 5, sut.Rect.CenterX, sut.Rect.Y + 5); // Touch scroll
			for (var i = 0; i < 10; i++) _app.TapCoordinates(sut.Rect.Right - 5, sut.Rect.Bottom - 5); // Mouse scroll

			var final = TakeScreenshot("final");

			ImageAssert.AreEqual(initial, final, sut.Rect);
		}
	}
}
