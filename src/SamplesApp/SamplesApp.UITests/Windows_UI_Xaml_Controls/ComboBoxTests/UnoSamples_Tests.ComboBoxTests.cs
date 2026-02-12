using System;
using System.Drawing;
using System.Linq;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ComboBoxTests
{
	[TestFixture]
	public partial class ComboBoxTests_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)]
		public void ComboBoxTests_PickerDefaultValue()
		{
			Run("SamplesApp.Wasm.Windows_UI_Xaml_Controls.ComboBox.ComboBox_Picker");

			_app.WaitForElement(_app.Marked("theComboBox"));
			var theComboBox = _app.Marked("theComboBox");

			Assert.IsNull(theComboBox.GetDependencyPropertyValue("SelectedItem"));
		}

		[Test]
		[AutoRetry]
		public void ComboBoxTests_Kidnapping()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ComboBox.ComboBox_ComboBoxItem_Selection");

			var scrollingInitial = Configuration.AttemptToFindTargetBeforeScrolling;
			Configuration.AttemptToFindTargetBeforeScrolling = true; //Needed on WASM because ScrollUpTo() currently not implemented
			var comboBox = _app.Marked("_combo2");
			_app.WaitForElement(comboBox);
			var presenter = _app.FindWithin(marked: "ContentPresenter", within: comboBox);

			var button = _app.Marked("ChangeSelectionButton");
			_app.FastTap(button);

			var first = _app.FindWithin("_tb1", presenter);
			Assert.AreEqual("Item 1", first.GetDependencyPropertyValue<string>("Text"));

			_app.FastTap(comboBox);
			_app.TapCoordinates(300, 100);
			first = _app.FindWithin("_tb1", presenter);
			Assert.AreEqual("Item 1", first.GetDependencyPropertyValue<string>("Text"));

			_app.FastTap(button);
			var second = _app.FindWithin("_tb2", presenter);
			Assert.AreEqual("Item 2", second.GetDependencyPropertyValue<string>("Text"));

			// Close the combo box to not pollute other tests ...
			_app.TapCoordinates(10, 10);

			Configuration.AttemptToFindTargetBeforeScrolling = scrollingInitial;
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.Browser)] // Disabled for iOS: https://github.com/unoplatform/uno/issues/1955
		public void ComboBoxTests_VisibleBounds()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ComboBox.ComboBox_VisibleBounds");

			var combo01 = _app.Marked("combo01");
			var sampleControl = _app.Marked("sampleControl");
			var changeExtended = _app.Marked("changeExtended");

			var resourcesFilterResult = _app.WaitForElement(combo01).First();
			var sampleControlResult = _app.WaitForElement(sampleControl).First();

			_app.FastTap(combo01);

			var popupResult = _app.WaitForElement("PopupBorder").First();

			var popupLocationDifference = popupResult.Rect.Y - resourcesFilterResult.Rect.Bottom;

			_app.TapCoordinates(popupResult.Rect.Y - 10, popupResult.Rect.X);

			_app.FastTap(changeExtended);

			var resourcesFilterResultExtended = _app.WaitForElement(combo01).First();
			var sampleControlResultExtended = _app.WaitForElement(sampleControl).First();

			_app.FastTap(combo01);

			var popupResultExtended = _app.WaitForElement("PopupBorder").First();

			var popupLocationDifferenceExtended = popupResultExtended.Rect.Y - resourcesFilterResultExtended.Rect.Bottom;

			Assert.AreEqual(popupLocationDifferenceExtended, 2, 1);

			// Validates that the popup has not moved. The use of sampleControlResultExtended
			// compensates for a possible change of origins with android popups.
			Assert.AreEqual(popupLocationDifference - sampleControlResultExtended.Rect.Y, popupLocationDifferenceExtended);
		}

		[Test]
		[AutoRetry]
		public void ComboBoxTests_Stretch()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ComboBox.ComboBox_Stretch");

			var combo01 = _app.Marked("combo01");
			var sampleControl = _app.Marked("sampleControl");

			var sampleControlResult = _app.WaitForElement(sampleControl).First();

			_app.FastTap(combo01);

			var popupResult = _app.WaitForElement("PopupBorder").First();

			Assert.Less(popupResult.Rect.Width, sampleControlResult.Rect.Width / 2, "The popup should not stretch to the width of the screen");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.Browser)] // Ignore iOS for timeout using Xamarin.UITest 3.2 (or iOS 15) https://github.com/unoplatform/uno/issues/8013
		public void ComboBoxTests_Fullscreen_Popup_Generic()
		{
			Run("SamplesApp.Wasm.Windows_UI_Xaml_Controls.ComboBox.ComboBox_FullScreen_Popup");

			var values2 = _app.Marked("Values2");
			var sampleControl = _app.Marked("sampleControl");

			var sampleControlResult = _app.WaitForElement(sampleControl).First();

			_app.FastTap(values2);

			var popupResult = _app.WaitForElement("PopupBorder").First();

			TakeScreenshot("Opened");

			Assert.AreEqual(popupResult.Rect.Width, sampleControlResult.Rect.Width, "The popup must stretch horizontally");
			Assert.Less(popupResult.Rect.Height, sampleControlResult.Rect.Height / 2, "The popup should not stretch to the height of the screen");

			_app.TapCoordinates(sampleControlResult.Rect.Width / 2, popupResult.Rect.Bottom + 20);

			_app.WaitForNoElement("PopupBorder");

			TakeScreenshot("Closed");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)]
		public void ComboBoxTests_Fullscreen_Popup_iOS()
		{
			Run("SamplesApp.Wasm.Windows_UI_Xaml_Controls.ComboBox.ComboBox_FullScreen_Popup");

			var values2 = _app.Marked("Units1");
			var sampleControl = _app.Marked("sampleControl");

			var sampleControlResult = _app.WaitForElement(sampleControl).First();

			_app.FastTap(values2);

			TakeScreenshot("Opened");

			var popupResult = _app.WaitForElement("PopupBorder").First();

			Assert.AreEqual(popupResult.Rect.Width, sampleControlResult.Rect.Width, "The popup must stretch horizontally");
			Assert.Less(popupResult.Rect.Height, sampleControlResult.Rect.Height / 2, "The popup should not stretch to the height of the screen");

			_app.TapCoordinates(sampleControlResult.Rect.Width / 2, popupResult.Rect.Y - 20);

			_app.WaitForNoElement("PopupBorder");

			TakeScreenshot("Closed");
		}

		[Test]
		[AutoRetry]
		public void ComboBoxTests_Disabled()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ComboBox.ComboBox_Disabled");

			_app.WaitForElement(_app.Marked("DisabledComboBox"));
			var disabledComboBox = _app.Marked("DisabledComboBox");

			Assert.IsFalse(disabledComboBox.GetDependencyPropertyValue<bool>("IsEnabled"));

			_app.WaitForElement(_app.Marked("HeaderContentPresenter"));
			var headerContentPresenter = _app.Marked("HeaderContentPresenter");

			Assert.AreEqual("Test Disabled ComboBox", headerContentPresenter.GetDependencyPropertyValue("Content"));
		}

		[Test]
		[AutoRetry]
		public void ComboBoxTests_ToggleDisabled()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ComboBox.ComboBox_ToggleDisabled");

			_app.WaitForElement(_app.Marked("DisablingComboBox"));
			var disablingComboBox = _app.Marked("DisablingComboBox");

			TakeScreenshot("ComboBox Enabled");

			_app.FastTap("ToggleDisabledButton");

			_app.WaitForText("IsEnabledComboBox", "False");

			Assert.IsFalse(disablingComboBox.GetDependencyPropertyValue<bool>("IsEnabled"));

			_app.WaitForElement(_app.Marked("HeaderContentPresenter"));
			var headerContentPresenter = _app.Marked("HeaderContentPresenter");

			Assert.AreEqual("Test Toggle Disabled ComboBox", headerContentPresenter.GetDependencyPropertyValue("Content"));

			TakeScreenshot("ComboBox Disabled");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.Browser)] // For some reason WaitForText() fails to even find the TextBlock on iOS
		public void ComboBox_Dropdown_Background()
		{
			var isCurrentlyOpen = false;

			Run("UITests.Windows_UI_Xaml_Controls.ComboBox.ComboBox_Dropdown_Background_4418");

			_app.WaitForElement("IsOpenTextBlock");
			ToggleComboBox();
			ToggleComboBox();

			ToggleComboBox();
			ToggleComboBox();

			ToggleComboBox();
			// Third time's the bug

			var scrn = TakeScreenshot("ComboBox open");
			var rect = _app.GetPhysicalRect("ViewfinderBorder");

			ImageAssert.HasColorAt(scrn, rect.CenterX, rect.CenterY, Color.Tomato);

			ToggleComboBox();

			void ToggleComboBox()
			{
				_app.FastTap("YeComboBox");
				isCurrentlyOpen = !isCurrentlyOpen;
				_app.WaitForText("IsOpenTextBlock", isCurrentlyOpen.ToString());
			}
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android)]
		public void ComboBoxTests_PlaceholderText() => ComboBoxTests_PlaceholderText_Impl("TestBox", 4, combo =>
		{
			var implicitTextBlock = combo.Descendant().Marked("ContentPresenter").Child;
			var text = implicitTextBlock.GetDependencyPropertyValue<string>("Text");

			Assert.AreEqual("5", text, "item #4 (text: 5) should be now selected");
		});

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android)]
		public void ComboBoxTests_PlaceholderText_With_ItemTemplate() => ComboBoxTests_PlaceholderText_Impl("TestBox2", 4, combo =>
		{
			var descendants = combo.Descendant().Marked("ContentPresenter").Descendant();
			//<StackPanel Orientation="Horizontal"><!-- templateRoot -->
			//	<Rectangle ... />
			//	<TextBlock Text="Item:" />
			//	<TextBlock Text="{Binding}" />
			//</StackPanel>
			var offset = 2; // skipping the ContentPresenter#0 and the StackPanel#1
			var text1 = descendants.AtIndex(offset + 1).GetDependencyPropertyValue<string>("Text");
			var text2 = descendants.AtIndex(offset + 2).GetDependencyPropertyValue<string>("Text");

			Assert.AreEqual("Item:", text1, "item #4 should be now selected with ItemTemplate");
			Assert.AreEqual("5", text2, "item #4 (value: 5) should be now selected with ItemTemplate");
		});

		[Test]
		[AutoRetry]
		public void ComboBox_With_Description()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ComboBox.ComboBox_Description", skipInitialScreenshot: true);
			var comboBox = _app.WaitForElement("DescriptionComboBox")[0];
			using var screenshot = TakeScreenshot("ComboBox Description", new ScreenshotOptions() { IgnoreInSnapshotCompare = true });
			ImageAssert.HasColorAt(screenshot, comboBox.Rect.X + comboBox.Rect.Width / 2, comboBox.Rect.Y + comboBox.Rect.Height - 150, Color.Red);
		}

		public void ComboBoxTests_PlaceholderText_Impl(string targetName, int selectionIndex, Action<QueryEx> selectionValidation)
		{
			Run("SamplesApp.Wasm.Windows_UI_Xaml_Controls.ComboBox.ComboBox_PlaceholderText");
			_app.WaitForElement("sampleControl");

			var testComboBox = _app.Marked(targetName);
			var resetButton = _app.Marked("ResetButton");

			// check initial value
			var placeholderTextBlock = testComboBox.Descendant().Marked("PlaceholderTextBlock");
			var expectedPlaceholderText = testComboBox.GetDependencyPropertyValue<string>("PlaceholderText");
			var actualPlaceholderText = placeholderTextBlock.GetDependencyPropertyValue<string>("Text");
			Assert.AreEqual(expectedPlaceholderText, actualPlaceholderText, "PlaceholderText should be shown prior to selection");

			// open combobox flyout
			testComboBox.FastTap();
			var popupBorder = _app.Marked("PopupBorder");
			_app.WaitForElement(popupBorder);

			// select item at {selectionIndex}
			var item5 = popupBorder.Descendant().WithClass("ComboBoxItem").AtIndex(selectionIndex);
			item5.FastTap();
			_app.WaitForDependencyPropertyValue<int>(testComboBox, "SelectedIndex", selectionIndex);
			selectionValidation(testComboBox);

			// clear selection
			resetButton.FastTap();
			_app.WaitForDependencyPropertyValue<int>(testComboBox, "SelectedIndex", -1);
			actualPlaceholderText = placeholderTextBlock.GetDependencyPropertyValue<string>("Text");
			Assert.AreEqual(expectedPlaceholderText, actualPlaceholderText, "PlaceholderText should be shown again when the selection is cleared");
		}
	}
}
