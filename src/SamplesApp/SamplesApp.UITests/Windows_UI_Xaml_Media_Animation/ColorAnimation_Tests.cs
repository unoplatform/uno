using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Media_Animation
{
	[TestFixture]
	public partial class ColorAnimation_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.Browser)] // Disabled for iOS because HasColor() behaves strangely: https://github.com/unoplatform/uno/issues/1955
		public void When_Border_Background_Animated()
		{
			Run("UITests.Windows_UI_Xaml_Media_Animation.ColorAnimation_Background");

			_app.WaitForElement("PlayColorAnimation");

			_app.FastTap("BrushEqualityButton");

			_app.WaitForText("BrushEqualityText", "true");

			_app.FastTap("PlayColorAnimation");

			_app.WaitForText("StatusText", "Completed");

			_app.FastTap("BrushEqualityButton");

			_app.WaitForText("BrushEqualityText", "false");

			var targetRect = _app.GetRect("TargetBorder");

			var indepRect = _app.GetRect("IndependentBorder");

			using var bmp = TakeScreenshot("Completed");

			ImageAssert.HasColorAt(bmp, targetRect.CenterX, targetRect.CenterY, Color.Red);

			ImageAssert.HasColorAt(bmp, indepRect.CenterX, indepRect.CenterY, Color.Blue); //Shared resource shouldn't be modified
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.Browser)] // Disabled for iOS because HasColor() behaves strangely: https://github.com/unoplatform/uno/issues/1955
		public void When_Rectangle_Fill_Animated()
		{
			Run("UITests.Windows_UI_Xaml_Media_Animation.ColorAnimation_Fill");

			_app.WaitForElement("PlayColorAnimation");

			_app.FastTap("PlayColorAnimation");

			_app.WaitForText("StatusText", "Completed");

			var targetRect = _app.GetRect("TargetRectangle");

			using var bmp = TakeScreenshot("Completed");

			ImageAssert.HasColorAt(bmp, targetRect.CenterX, targetRect.CenterY, Color.Brown);
		}
	}
}
