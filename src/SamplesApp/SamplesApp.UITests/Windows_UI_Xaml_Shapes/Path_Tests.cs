using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Shapes
{
	[TestFixture]
	public partial class Path_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // EvenOdd fill not working on Android and iOS - https://github.com/unoplatform/uno/issues/6703
		public void Test_FillRule()
		{
			Run("UITests.Windows_UI_Xaml_Shapes.PathTestsControl.Path_FillRule");

			_app.WaitForElement("MainTargetEvenOdd");

			var scrn = TakeScreenshot("Rendered", true);

			AssertHasColorAtCenter("MainTargetEvenOdd", Color.Beige);
			AssertHasColorAtCenter("RightTargetEvenOdd", Color.Green);

			AssertHasColorAtCenter("MainTargetNonZero", Color.Blue);
			AssertHasColorAtCenter("RightTargetNonZero", Color.Blue);

			void AssertHasColorAtCenter(string element, Color color)
			{
				var rect = _app.GetPhysicalRect(element);
				ImageAssert.HasColorAt(scrn, rect.CenterX, rect.CenterY, color);
			}
		}
	}
}
