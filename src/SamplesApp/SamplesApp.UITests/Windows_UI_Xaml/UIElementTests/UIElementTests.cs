using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;

namespace SamplesApp.UITests.Windows_UI_Xaml
{
	[TestFixture]
	public class UIElementTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void When_TransformToVisual()
		{
			Run("UITests.Shared.Windows_UI_Xaml.UIElementTests.TransformToVisual_Transform");

			_app.WaitForText("IsLoadedText", "Loaded");

			var windowWidthText = _app.GetText("WindowWidth");
			var windowHeightText = _app.GetText("WindowHeight");

			var transform1XText = _app.GetText("Border1TransformNullX");
			var transform1YText = _app.GetText("Border1TransformNullY");

			const int borderWidth = 50;
			const int borderHeight = 50;

			var windowWidth = int.Parse(windowWidthText);
			var windowHeight = int.Parse(windowHeightText);
			var transformX = int.Parse(transform1XText);
			var transformY = int.Parse(transform1YText);

			Assert.Greater(windowWidth, 100);
			Assert.Greater(windowHeight, 100);

			Assert.AreEqual(windowWidth - borderWidth, transformX);
			Assert.AreEqual(windowHeight - borderHeight, transformY);
		}

		// Will be converted to RuntimeTests
		//[Test]
		//[AutoRetry]
		//public void When_TransformToVisual_Transform()
		//{
		//	Run("UITests.Shared.Windows_UI_Xaml.UIElementTests.TransformToVisual_Transform");

		//	_app.WaitForText("IsLoadedText", "Loaded");

		//	var windowWidthText = _app.GetText("WindowWidth");
		//	var windowHeightText = _app.GetText("WindowHeight");

		//	var transform2XText = _app.GetText("Border2TransformNullX");
		//	var transform2YText = _app.GetText("Border2TransformNullY");

		//	const int borderWidth = 50;
		//	const int borderHeight = 50;
		//	const int translateX = 50;
		//	const int translateY = 50;

		//	var windowWidth = int.Parse(windowWidthText);
		//	var windowHeight = int.Parse(windowHeightText);
		//	var transformX = int.Parse(transform2XText);
		//	var transformY = int.Parse(transform2YText);

		//	Assert.Greater(windowWidth, 100);
		//	Assert.Greater(windowHeight, 100);

		//	Assert.AreEqual(windowWidth - borderWidth - translateX, transformX);
		//	Assert.AreEqual(windowHeight - borderHeight - translateY, transformY);

		//	TakeScreenshot(nameof(When_TransformToVisual_Transform));
		//}
	}
}
