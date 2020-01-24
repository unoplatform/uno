using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Shapes
{
	public class Shapes_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void Draw_polyline()
		{
			Run("SamplesApp.Windows_UI_Xaml_Shapes.PolylinePage");
			_app.WaitForElement("DPolyline");
			TakeScreenshot($"PolylinePage");
			TabWaitAndThenScreenshot("ChangeShape");

			void TabWaitAndThenScreenshot(string buttonName)
			{
				_app.Marked(buttonName).Tap();
				_app.WaitForElement("DPolyline");
				TakeScreenshot($"PolylinePage - {buttonName}");
			}
		}

		[Test]
		[AutoRetry]
		public void Draw_polygon()
		{
			Run("SamplesApp.Windows_UI_Xaml_Shapes.PolygonPage");
			_app.WaitForElement("DPolygon");
			TakeScreenshot($"PolygonPage");
			TabWaitAndThenScreenshot("ChangeShape");

			void TabWaitAndThenScreenshot(string buttonName)
			{
				_app.Marked(buttonName).Tap();
				_app.WaitForElement("DPolygon");
				TakeScreenshot($"PolygonPage - {buttonName}");
			}
		}

		[Test]
		[AutoRetry]
		public void Affect_Measurement_polygon()
		{
			Run("SamplesApp.Windows_UI_Xaml_Shapes.PolygonPage");

			_app.WaitForElement("DPolygon");
			_app.Marked("ClearShape").Tap();
			TakeScreenshot($"PolygonPage - ClearShape");

			_app.Marked("ChangeShape").Tap();
			var widthzize = _app.Query(_app.Marked("DPolygon")).First().Rect.Width;

			if (widthzize == 0)
				Assert.Fail("Shape not changed");

			TakeScreenshot($"PolygonPage - ChangeShape-After clear");
		}

		[Test]
		[AutoRetry]
		public void Affect_Measurement_polyline()
		{
			Run("SamplesApp.Windows_UI_Xaml_Shapes.PolylinePage");

			_app.WaitForElement("DPolyline");
			_app.Marked("ClearShape").Tap();
			TakeScreenshot($"PolylinePage - ClearShape");

			_app.Marked("ChangeShape").Tap();
			var widthzize = _app.Query(_app.Marked("DPolyline")).First().Rect.Width;

			if(widthzize == 0)
				Assert.Fail("Shape not changed");

			TakeScreenshot($"PolylinePage - ChangeShape-After clear");
		}

		[Test]
		[AutoRetry]
		public void Ellipse_Page()
		{
			Run("SamplesApp.Windows_UI_Xaml_Shapes.EllipsePage");
			_app.WaitForElement("ellipse0");

			var ellipse1 = _app.Marked("ellipse1").FirstResult().Rect;
			var ellipse2 = _app.Marked("ellipse2").FirstResult().Rect;
			var ellipse3 = _app.Marked("ellipse3").FirstResult().Rect;
			var ellipse4 = _app.Marked("ellipse4").FirstResult()?.Rect;

			ellipse2.Width.Should().Be(ellipse1.Width, "Invalid ellipse2 width");
			ellipse3.Width.Should().Be(ellipse1.Width, "Invalid ellipse3 width");
			ellipse4?.Width.Should().Be(0, "Invalid ellipse4 width");

			ellipse2.Height.Should().Be(ellipse1.Height, "Invalid ellipse2 height");
			ellipse3.Height.Should().Be(ellipse1.Height, "Invalid ellipse3 height");
			ellipse4?.Height.Should().Be(ellipse1.Height, "Invalid ellipse4 height");
		}

		[Test]
		[AutoRetry]
		public void Draw_line()
		{
			Run("SamplesApp.Windows_UI_Xaml_Shapes.LinePage");
			_app.WaitForElement("DLinePage");
			TakeScreenshot($"LinePage");
		}

		[Test]
		[AutoRetry]
		public void Check_Bound_Color()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Shapes.Rectangle_Color_Bound");

			_app.WaitForText("StatusTextBlock", "Is bound");

			var screenshot = TakeScreenshot("Complete");

			var bounds = _app.GetRect("TargetRectangle");

			ImageAssert.HasColorAt(screenshot, bounds.CenterX, bounds.CenterY, Color.Blue);
		}
	}
}
