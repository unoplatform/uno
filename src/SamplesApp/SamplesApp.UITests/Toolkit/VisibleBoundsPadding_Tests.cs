using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.Extensions;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Toolkit
{
	[TestFixture]
	public partial class VisibleBoundsPadding_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)]
		public void When_Inside_Modal()
		{
			const string Blue = "#0000FF";

			Run("UITests.Toolkit.VisibleBoundsPadding_Modal_Test");

			App.FastTap("SafeArea_Launch_Modal_Button");

			App.WaitForElement("ContainerGrid");
			App.FastTap("ChangeLayoutButton");

			var containerGrid = App.GetPhysicalRect("ContainerGrid");
			var testRectangle = App.GetPhysicalRect("TestRectangle");

			using var screenshot = TakeScreenshot("SafeArea_Modal_After_Relayout");

			var safeAreaBottomInset = containerGrid.Bottom - testRectangle.Bottom;
			Assert.Greater(safeAreaBottomInset, 0);

			for (int i = 1; i < safeAreaBottomInset; i++)
			{
				ImageAssert.HasPixels(
					screenshot,
					ExpectedPixels
						.At(containerGrid.CenterX / 2, containerGrid.Bottom - i)
						.Named($"SafeArea_Modal_Bottom_{i}")
						.Pixel(Blue));
			}

			App.FastTap("CloseModalButton");
		}
	}
}
