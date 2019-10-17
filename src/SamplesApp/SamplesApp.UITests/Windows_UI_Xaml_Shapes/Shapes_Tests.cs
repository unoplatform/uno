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
			_app.Screenshot($"PolylinePage");
			TabWaitAndThenScreenshot("ChangeShape");

			void TabWaitAndThenScreenshot(string buttonName)
			{
				_app.Marked(buttonName).Tap();
				_app.Wait(TimeSpan.FromSeconds(2));
				_app.Screenshot($"PolylinePage - {buttonName}");
			}
		}

		[Test]
		[AutoRetry]
		public void Draw_polygon()
		{
			Run("SamplesApp.Windows_UI_Xaml_Shapes.PolygonPage");
			_app.WaitForElement("DPolygon");
			_app.Screenshot($"PolygonPage");
			TabWaitAndThenScreenshot("ChangeShape");

			void TabWaitAndThenScreenshot(string buttonName)
			{
				_app.Marked(buttonName).Tap();
				_app.Wait(TimeSpan.FromSeconds(2));
				_app.Screenshot($"PolygonPage - {buttonName}");
			}
		}

		[Test]
		[AutoRetry]
		public void Draw_ellipse()
		{
			Run("SamplesApp.Windows_UI_Xaml_Shapes.EllipsePage");
			_app.WaitForElement("DEllipsePage");
			_app.Screenshot($"EllipsePage");
		}

		[Test]
		[AutoRetry]
		public void Draw_line()
		{
			Run("SamplesApp.Windows_UI_Xaml_Shapes.LinePage");
			_app.WaitForElement("DLinePage");
			_app.Screenshot($"LinePage");
		}
	}
}
