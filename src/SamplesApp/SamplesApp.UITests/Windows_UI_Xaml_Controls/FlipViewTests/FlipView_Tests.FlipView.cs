using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using System;
using System.Linq;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.FlipViewTests
{
    [TestFixture]
    public partial class FlipView_Tests : SampleControlUITestBase
    {
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void FlipView_WithButtons_FlipForward()
		{
			Run("UITests.Windows_UI_Xaml_Controls.FlipView.FlipView_Buttons");

			_app.WaitForElement("NextButtonHorizontal");

			_app.WaitForElement("Button1");

			_app.WaitForElement("NextButtonHorizontal");

			// FlipView buttons are Collapsed by default.
			var nextButton = _app.Marked("NextButtonHorizontal");			
			nextButton.SetDependencyPropertyValue("Visibility", "Visible");

			_app.FastTap("NextButtonHorizontal");

			_app.WaitForElement("Button2");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void FlipView_WithButtons_FlipBackward()
		{
			Run("UITests.Windows_UI_Xaml_Controls.FlipView.FlipView_Buttons");

			_app.WaitForElement("NextButtonHorizontal");

			// FlipView buttons are Collapsed by default.
			var nextButton = _app.Marked("NextButtonHorizontal");
			nextButton.SetDependencyPropertyValue("Visibility", "Visible");

			_app.FastTap("NextButtonHorizontal");

			_app.WaitForElement("Button2");

			_app.WaitForElement("PreviousButtonHorizontal");

			var backButton = _app.Marked("PreviousButtonHorizontal");
			backButton.SetDependencyPropertyValue("Visibility", "Visible");
			
			_app.FastTap("PreviousButtonHorizontal");

			_app.WaitForElement("Button1");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS | Platform.Android)]
		public void FlipView_WithButtons_FlipForward_Swipe()
		{
			Run("UITests.Windows_UI_Xaml_Controls.FlipView.FlipView_Buttons");

			QueryEx sut = "SUT";

			var sutRect = _app.Query(sut).Single().Rect;

			_app.WaitForElement("Button1");

			_app.DragCoordinates(sutRect.CenterX, sutRect.CenterY, sutRect.X + 10, sutRect.CenterY);

			_app.WaitForElement("Button2");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS | Platform.Android)]
		public void FlipView_WithButtons_FlipBackward_Swipe()
		{
			Run("UITests.Windows_UI_Xaml_Controls.FlipView.FlipView_Buttons");

			QueryEx sut = "SUT";

			var sutRect = _app.Query(sut).Single().Rect;

			_app.WaitForElement("Button1");

			_app.DragCoordinates(sutRect.CenterX, sutRect.CenterY, sutRect.X + 10, sutRect.CenterY);

			_app.WaitForElement("Button2");

			_app.DragCoordinates(sutRect.CenterX, sutRect.CenterY, sutRect.X - 10, sutRect.CenterY);

			_app.WaitForElement("Button1");
		}
	}
}
