using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;

namespace SamplesApp.UITests.Windows_UI_Xaml_Media.SolidColorBrushTests
{
	[TestFixture]
	public partial class SolidColorBrush_Tests : SampleControlUITestBase
	{

		[Test]
		[AutoRetry]
		public void When_SolidColorBrush_Color_Changed()
		{
			Run("UITests.Windows_UI_Xaml_Media.BrushesTests.SolidColorBrush_Color_Changed");

			_app.WaitForElement("StatusTextBlock");

			var data = new (string View, Color InitialColor)[]
			{
				("ToggleableBorder", Color.Green),
				("ToggleableGrid", Color.IndianRed),
				("ToggleableEllipse", Color.Violet),
				("ToggleableFillEllipse", Color.DarkGoldenrod),
			};

			using var initial = TakeScreenshot("Initial");
			foreach (var (view, initialColor) in data)
			{
				var rect = _app.GetPhysicalRect(view + "Viewfinder");
				ImageAssert.HasColorAt(initial, rect.CenterX, rect.CenterY, initialColor);
			}

			_app.FastTap("ChangeBorderColorButton");
			_app.WaitForText("StatusTextBlock", "Set");

			using var borderChanged = TakeScreenshot("Borders changed");
			foreach (var (view, _) in data)
			{
				var rect = _app.GetPhysicalRect(view + "Viewfinder");
				ImageAssert.HasColorAt(borderChanged, rect.CenterX, rect.CenterY, Color.Blue);
			}
		}
	}
}
