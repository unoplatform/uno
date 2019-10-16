using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;

namespace SamplesApp.UITests.Windows_UI_Xaml_Shapes
{
	public class Polyline_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void Draw_polyline()
		{
			Run("SamplesApp.Windows_UI_Xaml_Shapes.PolylinePage");

			_app.Screenshot("Polyline - Result");
		}
	}
}
