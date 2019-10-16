using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;

namespace SamplesApp.UITests.Windows_UI_Xaml_Shapes
{
	public class Polygon_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void Draw_polygon()
		{
			Run("SamplesApp.Windows_UI_Xaml_Shapes.PolylonPage");

			_app.Screenshot("Polylon - Result");
		}
	}
}
