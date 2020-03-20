using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.MenuFlyoutTests
{
	[TestFixture]
	class MenuFlyoutTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void MenuFlyoutItem_ClickTest()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.MenuFlyoutTests.MenuFlyoutItem_Click");

			_app.WaitForElement(_app.Marked("mfiButton"));

			TakeScreenshot("Initial");

			// step 1: press button to show menu
			_app.FastTap(_app.Marked("mfiButton"));

			TakeScreenshot("menuShown");

			// step 2: click MenuFlyoutItem
			_app.FastTap(_app.Marked("mfiItem"));

			// step 3: check result
			_app.WaitForText(_app.Marked("mfiResult"), "success");

			TakeScreenshot("AfterSuccess");
		}

		[Test]
		[AutoRetry]
		public void Simple_MenuFlyout()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.MenuBarTests.SimpleMenuBar");

			_app.WaitForElement(_app.Marked("fileMenu"));
			var result = _app.Marked("result");

			TakeScreenshot("Initial");

			void Validate(string topMenu, string item, string expectedResult)
			{
				_app.FastTap(_app.Marked(topMenu));

				TakeScreenshot(item);

				_app.FastTap(_app.Marked(item));

				_app.WaitForText(result, expectedResult);
			}

			Validate("fileMenu", "exitMenu", "click text:Exit");
			Validate("fileMenu", "openMenu", "click text:Open...");
			Validate("fileMenu", "xamlUIMenu", "xamluicommand param:xamlUIMenu");

			TakeScreenshot("AfterSuccess");
		}

		[Test]
		[AutoRetry]
		public void Simple_SubMenuFlyout()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.MenuBarTests.SimpleMenuBar");

			_app.WaitForElement(_app.Marked("fileMenu"));
			var result = _app.Marked("result");

			TakeScreenshot("Initial");

			_app.FastTap(_app.Marked("fileMenu"));

			TakeScreenshot("fileMenu");

			_app.FastTap(_app.Marked("newMenu"));

			TakeScreenshot("newMenu");

			_app.FastTap(_app.Marked("plainTextMenu"));

			_app.WaitForText(result, "command param:Plain Text");

			TakeScreenshot("AfterSuccess");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser, Platform.iOS)]
		public void Disabled_MenuFlyoutItem()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.MenuBarTests.SimpleMenuBar");

			_app.WaitForElement(_app.Marked("fileMenu"));
			var result = _app.Marked("result");

			TakeScreenshot("Initial");

			_app.FastTap(_app.Marked("fileMenu"));

			TakeScreenshot("fileMenu");

			_app.FastTap(_app.Marked("disabledItem"));

			TakeScreenshot("disabledItem");

			Task.Delay(250);

			_app.WaitForElement("disabledItem");

			TakeScreenshot("AfterSuccess");
		}

		[Test]
		[AutoRetry]
		public void Dismiss_MenuFlyout()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.MenuBarTests.SimpleMenuBar");

			_app.WaitForElement(_app.Marked("fileMenu"));
			var result = _app.Marked("result");

			TakeScreenshot("Initial");

			_app.FastTap(_app.Marked("fileMenu"));

			TakeScreenshot("fileMenu");

			var exitItemResult = _app.Query(_app.Marked("exitMenu")).First();

			_app.TapCoordinates(0, exitItemResult.Rect.Bottom + 20);

			_app.WaitForNoElement("exitItem");

			TakeScreenshot("AfterSuccess");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)] // We cannot test right button click on WASM yet
		public void UIElement_ContextFlyout()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.MenuFlyoutTests.UIElement_ContextFlyout");

			var result = _app.Marked("result");
			var myBorder = _app.Marked("myBorder");

			var myBorderRect = _app.Query(_app.Marked("myBorder")).First().Rect;
			_app.TouchAndHoldCoordinates(myBorderRect.CenterX, myBorderRect.CenterY);

			TakeScreenshot("opened");

			_app.FastTap(_app.Marked("testItem1"));
			Assert.AreEqual("click: test1", result.GetText());

			_app.TouchAndHoldCoordinates(myBorderRect.CenterX, myBorderRect.CenterY);

			_app.FastTap(_app.Marked("testItem2"));
			Assert.AreEqual("click: test2", result.GetText());
		}
	}
}
