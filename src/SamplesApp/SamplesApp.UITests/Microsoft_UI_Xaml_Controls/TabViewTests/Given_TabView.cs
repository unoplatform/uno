using System;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Microsoft_UI_Xaml_Controls.TabViewTests
{
	[Ignore("Tests are not yet stabilized")]
	public partial class Given_TabView : SampleControlUITestBase
	{
		// Missing tests
		// - KeyboardTest (requires keyboard support)
		// - GamePadTest (requires gamepad support)
		// - DragBetweenTabViewsTest (requires dragging)
		// - ReorderItemsTest (requires dragging)
		// - DragOutsideTest (requires dragging)
		// - TabSizeAndScrollButtonsTest (requires selecting items in combobox)
		// - CloseButtonOverlayModeTests (requires selecting items in combobox)
		// - HandleItemCloseRequestedTest (close button search)
		// - IsClosableTest (close button search)

		[SetUp]
		public void TestSetup()
		{
			Run("UITests.Microsoft_UI_Xaml_Controls.TabViewTests.TabViewPage");
		}

		[Test]
		[AutoRetry]
		public void SelectionTest()
		{
			{
				Console.WriteLine("Verify content is displayed for initially selected tab.");
				var tabContent = _app.Marked("FirstTabContent");
				Assert.NotNull(tabContent);

				Console.WriteLine("Changing selection.");
				var lastTab = _app.Marked("LastTab");
				lastTab.Tap();
				//Wait.ForIdle();

				Console.WriteLine("Verify content is displayed for newly selected tab.");
				tabContent = _app.Marked("LastTabContent");
				Assert.NotNull(tabContent);

				Console.WriteLine("Verify that setting SelectedItem changes selection.");
				var selectItemButton = _app.Marked("SelectItemButton");
				selectItemButton.Tap();

				var selectedIndexTextBlock = _app.Marked("SelectedIndexTextBlock");
				Assert.AreEqual("1", selectedIndexTextBlock.GetText());

				Console.WriteLine("Verify that setting SelectedIndex changes selection.");
				var selectIndexButton = _app.Marked("SelectIndexButton");
				selectIndexButton.Tap();
				Assert.AreEqual("2", selectedIndexTextBlock.GetText());

				//TODO: Uno UI Test does not support keyboard yet
				//Console.WriteLine("Verify that ctrl-click on tab selects it.");
				//UIObject firstTab = _app.Marked("FirstTab");
				//KeyboardHelper.PressDownModifierKey(ModifierKey.Control);
				//firstTab.Click();
				//KeyboardHelper.ReleaseModifierKey(ModifierKey.Control);
				////Wait.ForIdle();
				//Assert.AreEqual("0", selectedIndexTextBlock.GetText());

				//Console.WriteLine("Verify that ctrl-click on tab does not deselect.");
				//KeyboardHelper.PressDownModifierKey(ModifierKey.Control);
				//firstTab.Click();
				//KeyboardHelper.ReleaseModifierKey(ModifierKey.Control);
				////Wait.ForIdle();
				//Assert.AreEqual("0", selectedIndexTextBlock.GetText());
			}
		}

		[Test]
		[AutoRetry]
		public void AddRemoveTest()
		{
			{
				Console.WriteLine("Adding tab.");
				var addTabButton = _app.Marked("Add New Tab");
				addTabButton.Tap();

				//ElementCache.Refresh();
				var newTab = _app.Marked("New Tab 1");
				Assert.NotNull(newTab);

				Console.WriteLine("Removing tab.");
				var removeTabButton = _app.Marked("RemoveTabButton");
				removeTabButton.Tap();

				//ElementCache.Refresh();
				newTab = _app.Marked("New Tab 1");
				Assert.IsNull(newTab);
			}
		}

		private bool AreScrollButtonsVisible()
		{
			_app.Marked("GetScrollButtonsVisible").Tap();
			var scrollButtonsVisible = _app.Marked("ScrollButtonsVisible").GetText();
			if (scrollButtonsVisible == "True")
			{
				return true;
			}
			else if (scrollButtonsVisible == "False")
			{
				return false;
			}
			else
			{
				Assert.Fail(string.Format("Unexpected value for ScrollButtonsVisible: '{0}'", scrollButtonsVisible));
				return false;
			}
		}

		private bool IsScrollIncreaseButtonEnabled()
		{
			_app.Marked("GetScrollIncreaseButtonEnabled").Tap();
			var scrollIncreaseButtonEnabled = _app.Marked("ScrollIncreaseButtonEnabled").GetText();
			return scrollIncreaseButtonEnabled == "True";
		}

		private bool IsScrollDecreaseButtonEnabled()
		{
			_app.Marked("GetScrollDecreaseButtonEnabled").Tap();
			var scrollDecreaseButtonEnabled = _app.Marked("ScrollDecreaseButtonEnabled").GetText();
			return scrollDecreaseButtonEnabled == "True";
		}

		[Test]
		[AutoRetry]
		public void CloseSelectionTest()
		{
			{
				Console.WriteLine("Hiding the disabled tab");
				var disabledTabCheckBox = _app.Marked("IsDisabledTabVisibleCheckBox");
				Assert.NotNull(disabledTabCheckBox);
				disabledTabCheckBox.Tap(); // TODO: does this uncheck?

				Console.WriteLine("Finding the first tab");
				var firstTab = _app.Marked("FirstTab");

				//TODO: Uno close button search
				//var closeButton = FindCloseButton(firstTab);
				//Assert.NotNull(closeButton);

				var selectedIndexTextBlock = _app.Marked("SelectedIndexTextBlock");
				Assert.AreEqual("0", selectedIndexTextBlock.GetText());

				Console.WriteLine("When the selected tab is closed, selection should move to the next one.");
				// Use Tab's close button:
				TapCloseButton("FirstTab");
				var firstTabQuery = _app.Marked("FirstTab");
				Assert.AreEqual(0, firstTabQuery.Results().Length);
				Assert.AreEqual("0", selectedIndexTextBlock.GetText());

				Console.WriteLine("Select last tab.");
				var lastTab = _app.Marked("LastTab");
				lastTab.Tap();
				//Wait.ForIdle();
				Assert.AreEqual("3", selectedIndexTextBlock.GetText());

				Console.WriteLine("When the selected tab is last and is closed, selection should move to the previous item.");

				//TODO: Uno Middle mouse click
				//// Use Middle Click to close the tab:
				//lastTab.Tap(PointerButtons.Middle);
				////Wait.ForIdle();
				//VerifyElement.NotFound("LastTab", FindBy.Name);
				//Assert.AreEqual("2", selectedIndexTextBlock.GetText());
			}
		}

		[Test]
		[AutoRetry]
		public void AddButtonTest()
		{
			{
				Console.WriteLine("Add new tab button should be visible.");
				var addButton = _app.Marked("Add New Tab");
				Assert.NotNull(addButton);

				var isAddButtonVisibleCheckBox = _app.Marked("IsAddButtonVisibleCheckBox");
				// TODO: Uncheck
				isAddButtonVisibleCheckBox.Tap();
				//Wait.ForIdle();

				//ElementCache.Refresh();
				Console.WriteLine("Add new tab button should not be visible.");
				addButton = _app.Marked("Add New Tab");
				Assert.IsNull(addButton);
			}
		}

		[Test]
		[AutoRetry]
		public void ToolTipDefaultTest()
		{

			{
				Console.WriteLine("If the app sets custom tooltip text, it should be preserved.");
				PressButtonAndVerifyText("GetTab0ToolTipButton", "Tab0ToolTipTextBlock", "Custom Tooltip");

				Console.WriteLine("If the app does not set a custom tooltip, it should be the same as the header text.");
				PressButtonAndVerifyText("GetTab1ToolTipButton", "Tab1ToolTipTextBlock", "SecondTab");

				var changeShopTextButton = _app.Marked("ChangeShopTextButton");
				changeShopTextButton.Tap();

				Console.WriteLine("If the tab's header changes, the tooltip should update.");
				PressButtonAndVerifyText("GetTab1ToolTipButton", "Tab1ToolTipTextBlock", "Changed");
			}
		}

		[Test]
		[AutoRetry]
		public void ToolTipUpdateTest()
		{

			{
				var customTooltipButton = _app.Marked("CustomTooltipButton");
				customTooltipButton.Tap();

				Console.WriteLine("If the app updates the tooltip, it should change to their custom one.");
				PressButtonAndVerifyText("GetTab1ToolTipButton", "Tab1ToolTipTextBlock", "Custom");

				var changeShopTextButton = _app.Marked("ChangeShopTextButton");
				changeShopTextButton.Tap();

				Console.WriteLine("The tooltip should not update if the header changes.");
				PressButtonAndVerifyText("GetTab1ToolTipButton", "Tab1ToolTipTextBlock", "Custom");
			}
		}

		[Test]
		[AutoRetry]
		public async Task CloseButtonDoesNotShowWhenVisibilityIsToggled()
		{

			{
				// Wait for the test page's timer to set visibility to the close button to visible
				await Task.Delay(2);
				//Wait.ForIdle();

				var notCloseableTab = _app.Marked("NotCloseableTab");
				TapCloseButton(notCloseableTab);
				var newNotCloseableSearch = _app.Marked("NotCloseableTab");
				Assert.IsNull(newNotCloseableSearch);
			}
		}

		[Test]
		[AutoRetry]
		public async Task SizingTest()
		{

			{
				var sizingPageButton = _app.Marked("TabViewSizingPageButton");
				sizingPageButton.Tap();
				await Task.Delay(200);
				//ElementCache.Refresh();

				var setSmallWidthButton = _app.Marked("SetSmallWidth");
				setSmallWidthButton.Tap();

				var getWidthsButton = _app.Marked("GetWidthsButton");
				getWidthsButton.Tap();

				var widthEqualText = _app.Marked("WidthEqualText");
				var widthSizeToContentText = _app.Marked("WidthSizeToContentText");

				Assert.AreEqual("400", widthEqualText.GetText());
				Assert.AreEqual("400", widthSizeToContentText.GetText());

				var setLargeWidthButton = _app.Marked("SetLargeWidth");
				setLargeWidthButton.Tap();

				getWidthsButton.Tap();

				Assert.AreEqual("700", widthEqualText.GetText());
				Assert.AreEqual("700", widthSizeToContentText.GetText());
			}
		}

		[Test]
		[AutoRetry]
		public void ScrollButtonToolTipTest()
		{

			{
				PressButtonAndVerifyText("GetScrollDecreaseButtonToolTipButton", "ScrollDecreaseButtonToolTipTextBlock", "Scroll tab list backward");
				PressButtonAndVerifyText("GetScrollIncreaseButtonToolTipButton", "ScrollIncreaseButtonToolTipTextBlock", "Scroll tab list forward");
			}
		}

		[Test]
		[AutoRetry]
		public void VerifyTabViewItemHeaderForegroundResource()
		{

			{
				var getSecondTabHeaderForegroundButton = _app.Marked("GetSecondTabHeaderForegroundButton");
				getSecondTabHeaderForegroundButton.Tap();

				var secondTabHeaderForegroundTextBlock = _app.Marked("SecondTabHeaderForegroundTextBlock");

				Assert.AreEqual("#FF008000", secondTabHeaderForegroundTextBlock.GetText());
			}
		}

		[Test]
		[AutoRetry]
		public void VerifySizingBehaviorOnTabCloseComingFromScroll()
		{
			int pixelTolerance = 10;

			//using (var setup = new TestSetupHelper(new[] { "TabView Tests", "TabViewTabClosingBehaviorButton" }))
			_app.Marked("TabViewTabClosingBehaviorButton").Tap();

			{

				Console.WriteLine("Verifying sizing behavior when closing a tab");
				CloseTabAndVerifyWidth("Tab 1", 500, "True;False;");

				CloseTabAndVerifyWidth("Tab 2", 500, "True;False;");

				CloseTabAndVerifyWidth("Tab 3", 500, "False;False;");

				CloseTabAndVerifyWidth("Tab 5", 401, "False;False;");

				CloseTabAndVerifyWidth("Tab 4", 401, "False;False;");

				Console.WriteLine("Leaving the pointer exited area");
				var readTabViewWidthButton = _app.Marked("GetActualWidthButton");
				readTabViewWidthButton.Tap();
				//Wait.ForIdle();

				readTabViewWidthButton.Tap();
				//Wait.ForIdle();

				Console.WriteLine("Verify correct TabView width");
				Assert.IsTrue(Math.Abs(GetActualTabViewWidth() - 283) < pixelTolerance);
			}

			void CloseTabAndVerifyWidth(string tabName, int expectedValue, string expectedScrollbuttonStates)
			{
				Console.WriteLine("Closing tab:" + tabName);
				TapCloseButton(_app.Marked(tabName));
				//Wait.ForIdle();
				Console.WriteLine("Verifying TabView width");
				Assert.IsTrue(Math.Abs(GetActualTabViewWidth() - expectedValue) < pixelTolerance);
				Assert.AreEqual(expectedScrollbuttonStates, _app.Marked("ScrollButtonStatus").GetText());

			}

			double GetActualTabViewWidth()
			{
				var tabviewWidth = _app.Marked("TabViewWidth");

				return Double.Parse(tabviewWidth.GetText());
			}
		}

		[Test]
		[AutoRetry]
		public void VerifySizingBehaviorOnTabCloseComingFromNonScroll()
		{
			int pixelTolerance = 10;

			//using (var setup = new TestSetupHelper(new[] { "TabView Tests", "TabViewTabClosingBehaviorButton" }))
			_app.Marked("TabViewTabClosingBehaviorButton").Tap();

			{

				Console.WriteLine("Verifying sizing behavior when closing a tab");
				CloseTabAndVerifyWidth("Tab 1", 500, "True;False;");

				CloseTabAndVerifyWidth("Tab 2", 500, "True;False;");

				CloseTabAndVerifyWidth("Tab 3", 500, "False;False;");

				var readTabViewWidthButton = _app.Marked("GetActualWidthButton");
				readTabViewWidthButton.Tap();
				//Wait.ForIdle();

				CloseTabAndVerifyWidth("Tab 5", 500, "False;False;");

				CloseTabAndVerifyWidth("Tab 4", 500, "False;False;");

				Console.WriteLine("Leaving the pointer exited area");

				readTabViewWidthButton.Tap();
				//Wait.ForIdle();

				Console.WriteLine("Verify correct TabView width");
				Assert.IsTrue(Math.Abs(GetActualTabViewWidth() - 500) < pixelTolerance);
			}

			void CloseTabAndVerifyWidth(string tabName, int expectedValue, string expectedScrollbuttonStates)
			{
				Console.WriteLine("Closing tab:" + tabName);
				TapCloseButton(_app.Marked(tabName));
				//Wait.ForIdle();
				Console.WriteLine("Verifying TabView width");
				Assert.IsTrue(Math.Abs(GetActualTabViewWidth() - expectedValue) < pixelTolerance);
				Assert.AreEqual(expectedScrollbuttonStates, _app.Marked("ScrollButtonStatus").GetText());

			}

			double GetActualTabViewWidth()
			{
				var tabviewWidth = _app.Marked("TabViewWidth");

				return Double.Parse(tabviewWidth.GetText());
			}
		}

		public void PressButtonAndVerifyText(String buttonName, String textBlockName, String expectedText)
		{
			var button = _app.Marked(buttonName);
			button.Tap();

			var textBlock = _app.Marked(textBlockName);
			Assert.AreEqual(expectedText, textBlock.GetText());
		}

		private void TapCloseButton(QueryEx queryEx)
		{
			throw new NotImplementedException();
		}
	}
}
