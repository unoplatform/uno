#nullable disable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SamplesApp.UITests.Extensions;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;

namespace SamplesApp.UITests.Windows_UI_Xaml_Shapes
{
	public partial class Rectangle_Rounding : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android)]
		public void When_Height_Rounded()
		{
			Run("UITests.Windows_UI_Xaml_Shapes.Rectangle_Rounding");

			using var screenshot = TakeScreenshot($"Rectangle_Rounding");
			var container = _app.GetPhysicalRect($"GridContainer");

			for (float i = container.Y; i < container.Bottom; i++)
			{
				ImageAssert.HasColorAt(screenshot, container.CenterX, i, Color.Black);
			}
		}
	}
}
