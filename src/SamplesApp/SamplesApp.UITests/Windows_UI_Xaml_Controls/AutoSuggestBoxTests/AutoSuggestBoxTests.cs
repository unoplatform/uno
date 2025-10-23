using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.AutoSuggestBoxTests
{
	[TestFixture]
	public partial class AutoSuggestBoxTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void AutoSuggestBox_With_Description()
		{
			Run("UITests.Windows_UI_Xaml_Controls.AutoSuggestBoxTests.AutoSuggestBox_Description", skipInitialScreenshot: true);
			var autoSuggestBoxRect = ToPhysicalRect(_app.WaitForElement("DescriptionAutoSuggestBox")[0].Rect);
			using var screenshot = TakeScreenshot("AutoSuggestBox Description", new ScreenshotOptions() { IgnoreInSnapshotCompare = true });
			ImageAssert.HasColorAt(screenshot, autoSuggestBoxRect.X + autoSuggestBoxRect.Width / 2, autoSuggestBoxRect.Y + autoSuggestBoxRect.Height - 50, Color.Red);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Browser)] // Android not working currently. https://github.com/unoplatform/uno/issues/8836
		public void AutoSuggestBox_Reason()
		{
			Run("UITests.Windows_UI_Xaml_Controls.AutoSuggestBoxTests.AutoSuggestBox_Reason", skipInitialScreenshot: true);
			var SUT = _app.Marked("ReasonAutoSuggestBox");
			_app.WaitForElement(SUT);

			_app.FastTap("btnPopulate");
			_app.WaitForText("txtConsole", "ProgrammaticChange");
			//Entering Text as User
			SUT.EnterText("m");
			_app.WaitForText("txtConsole", "UserInput");

			//Missing SuggestionChosen
			//Uno UI Test does not support keyboard yet
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Browser)] // Android not working currently.
		public void BasicAutoSuggestBox()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.AutoSuggestBoxTests.BasicAutoSuggestBox", skipInitialScreenshot: true);
			var SUT = _app.Marked("box1");
			_app.WaitForElement(SUT);
			Assert.AreEqual(false, SUT.GetDependencyPropertyValue<bool>("IsSuggestionListOpen"));

			SUT.EnterText("a");

			Assert.AreEqual(true, SUT.GetDependencyPropertyValue<bool>("IsSuggestionListOpen"));

			// Click away to lose focus.
			_app.FastTap("textChanged");

			Assert.AreEqual(false, SUT.GetDependencyPropertyValue<bool>("IsSuggestionListOpen"));
		}
	}
}
