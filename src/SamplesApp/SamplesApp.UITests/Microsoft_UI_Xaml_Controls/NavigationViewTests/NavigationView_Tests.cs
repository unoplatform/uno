using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Microsoft_UI_Xaml_Controls.NavigationViewTests
{
	public partial class NavigationView_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry()]
		[ActivePlatforms(Platform.iOS, Platform.Android)] // fails on WebAssembly
		public void NavigateBackAndForthBetweenMenuItemsAndSettings_Fluent()
		{
			Run("SamplesApp.Samples.Microsoft_UI_Xaml_Controls.NavigationViewTests.FluentStyle.FluentStyle_NavigationViewSample");

			_app.WaitForElement(_app.Marked("BasicNavigation"));

			var secondMenuItem = _app.Marked("SecondItem");
			secondMenuItem.FastTap();

			_app.WaitForElement("Page2NavViewContent");

			var togglePaneButton = _app.Marked("TogglePaneButton");
			togglePaneButton.FastTap();

			var firstMenuItem = _app.Marked("FirstItem");
			firstMenuItem.FastTap();

			_app.WaitForElement("Page1NavViewContent");
		}
	}
}
