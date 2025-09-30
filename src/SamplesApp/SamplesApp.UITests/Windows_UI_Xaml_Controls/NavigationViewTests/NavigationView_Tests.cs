using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.NavigationViewTests
{
	public partial class NavigationView_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry()]
		public void BasicNavigationView()
		{
			Run("SamplesApp.Samples.Windows_UI_Xaml_Controls.NavigationViewTests.NavigationView_Pane_Automated");

			var itemPlay = _app.Marked("Item Play");
			var itemSave = _app.Marked("Item Save");
			var selectedItemText = _app.Marked("selectedItemText");

			// Work around for Xamarin.Android not showing the icon (VS2017 only)
			var itemPlayRect = _app.Query(itemPlay).First().Rect;
			var itemSaveRect = _app.Query(itemSave).First().Rect;

			_app.TapCoordinates(itemPlayRect.X + 5, itemPlayRect.Y + 5);
			_app.WaitForDependencyPropertyValue(selectedItemText, "Text", "Play");

			_app.TapCoordinates(itemSaveRect.X + 5, itemSaveRect.Y + 5);
			_app.WaitForDependencyPropertyValue(selectedItemText, "Text", "Save");
		}

#if !HAS_UNO_WINUI
		[Test]
		[AutoRetry()]
		public void NavigateBackAndForthBetweenMenuItemsAndSettings()
		{
			Run("SamplesApp.Samples.Windows_UI_Xaml_Controls.NavigationViewTests.NavigationView_BasicNavigation");

			_app.WaitForElement(_app.Marked("BasicNavigation"));

			var secondMenuItem = _app.Marked("SecondItem");
			secondMenuItem.FastTap();

			_app.WaitForElement("Page2NavViewContent");


			var togglePaneButton = _app.Marked("TogglePaneButton");
			togglePaneButton.FastTap();

			var settingsItem = _app.Marked("SettingsNavPaneItem");
			settingsItem.FastTap();

			_app.WaitForElement("SettingsNavViewContent");

			togglePaneButton.FastTap();

			var firstMenuItem = _app.Marked("FirstItem");
			firstMenuItem.FastTap();

			_app.WaitForElement("Page1NavViewContent");
		}

		[Test]
		[AutoRetry()]
		public void NavigationView_OnLightDismiss_TogglePaneButton_IsSizedCorrectly()
		{
			Run("SamplesApp.Samples.NavigationViewSample.NavigationViewSample");

			var descendants = _app.Marked("nvSample").Descendant();

			var lightDismissLayer = descendants.Marked("LightDismissLayer");
			var dismissRect = _app.GetLogicalRect(lightDismissLayer);

			var paneRoot = descendants.Marked("PaneRoot");
			var paneRootRect = _app.GetLogicalRect(paneRoot);
			var togglePaneButton = descendants.Marked("TogglePaneButton");

			var togglePaneRect = _app.GetLogicalRect(togglePaneButton);
			paneRootRect.Width.Should().Be(togglePaneRect.Width, "when NavigationView is opened, PaneRoot and TogglePaneButton should shared the same width");

			// to light-dismiss the flyout, we need to tap the right side of LightDismissLayer that isnt occupied by PaneRoot
			var dismissibleArea = new
			{
				CenterX = (paneRootRect.GetRight() + dismissRect.GetRight()) / 2,
				CenterY = dismissRect.CenterY,
			};
			_app.TapCoordinates(dismissibleArea.CenterX, dismissibleArea.CenterY);

			// refresh because FirstResult snapshots values
			togglePaneRect = _app.GetLogicalRect(togglePaneButton);

			togglePaneRect.Width.Should().BeLessThan(paneRootRect.Width, "when NavigationView is closed, TogglePaneButton should not take the width of PaneRoot");
		}
#endif
	}
}
