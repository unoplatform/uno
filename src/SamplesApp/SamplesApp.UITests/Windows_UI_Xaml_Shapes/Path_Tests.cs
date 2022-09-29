#nullable disable

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
		public void Test_FillRule()
		{
			Run("UITests.Windows_UI_Xaml_Shapes.PathTestsControl.Path_FillRule");

			_app.WaitForElement("MainTargetEvenOdd");

			var scrn = TakeScreenshot("Rendered", true);

			AssertHasColorAtCenter("MainTargetEvenOdd", Color.Beige);
			AssertHasColorAtCenter("LeftTargetEvenOdd", Color.Beige);
			AssertHasColorAtCenter("RightTargetEvenOdd", Color.Green);
			AssertHasColorAtCenter("MainTargetEvenOdd2", Color.Beige);
			AssertHasColorAtCenter("LeftTargetEvenOdd2", Color.Beige);
			AssertHasColorAtCenter("RightTargetEvenOdd2", Color.Green);
			AssertHasColorAtCenter("MainTargetEvenOdd3", Color.Beige);
			AssertHasColorAtCenter("LeftTargetEvenOdd2", Color.Beige);
			AssertHasColorAtCenter("RightTargetEvenOdd3", Color.Green);

			AssertHasColorAtCenter("MainTargetNonZero", Color.Blue);
			AssertHasColorAtCenter("LeftTargetNonZero", Color.Beige);
			AssertHasColorAtCenter("RightTargetNonZero", Color.Blue);
			AssertHasColorAtCenter("MainTargetNonZero2", Color.Blue);
			AssertHasColorAtCenter("LeftTargetNonZero2", Color.Beige);
			AssertHasColorAtCenter("RightTargetNonZero2", Color.Blue);
			AssertHasColorAtCenter("MainTargetNonZero3", Color.Blue);
			AssertHasColorAtCenter("LeftTargetNonZero3", Color.Beige);
			AssertHasColorAtCenter("RightTargetNonZero3", Color.Blue);

			void AssertHasColorAtCenter(string element, Color color)
			{
				var rect = _app.GetPhysicalRect(element);
				ImageAssert.HasColorAt(scrn, rect.CenterX, rect.CenterY, color);
			}
		}
	}
}
