using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
		public void Draw_ellipse()
		{
			Run("SamplesApp.Windows_UI_Xaml_Shapes.EllipsePage");
			_app.WaitForElement("DEllipsePage");
			TakeScreenshot($"EllipsePage");
		}

		[Test]
		[AutoRetry]
		public void Draw_line()
		{
			Run("SamplesApp.Windows_UI_Xaml_Shapes.LinePage");
			_app.WaitForElement("DLinePage");
			TakeScreenshot($"LinePage");
		}
	}
}
