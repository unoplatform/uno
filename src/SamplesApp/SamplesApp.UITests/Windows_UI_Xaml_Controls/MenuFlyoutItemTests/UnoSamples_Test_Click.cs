using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.MenuFlyoutItemTests
{
	[TestFixture]
	class UnoSamples_Test_Click: SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)]
		public void MenuFlyoutItem_ClickTest()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.MenuFlyoutItemTests.MenuFlyoutItem_Click");

			_app.WaitForElement(_app.Marked("mfiButton"));

			TakeScreenshot("Initial");

			// step 1: press button to show menu
			_app.Tap(_app.Marked("mfiButton"));

			TakeScreenshot("menuShown", ignoreInSnapshotCompare: AppInitializer.GetLocalPlatform() == Platform.Android /*Menu animation is midflight*/);

			// step 2: click MenuFlyoutItem
			_app.Tap(_app.Marked("mfiItem"));

			// step 3: check result
			_app.WaitForText(_app.Marked("mfiResult"), "success");  

			TakeScreenshot("AfterSuccess", ignoreInSnapshotCompare: AppInitializer.GetLocalPlatform() == Platform.Android /*Status bar appears with clock*/);
		}
	}
}
