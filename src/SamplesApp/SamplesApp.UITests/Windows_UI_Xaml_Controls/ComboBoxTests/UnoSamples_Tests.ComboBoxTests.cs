using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

			Assert.IsTrue(popupResult.Rect.Width < sampleControlResult.Rect.Width / 2, "The popup should not stretch to the width of the screen");
		}

		[Test]
		[AutoRetry]
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
			Assert.IsTrue(popupResult.Rect.Height < sampleControlResult.Rect.Height / 2, "The popup should not stretch to the height of the screen");

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
			Assert.IsTrue(popupResult.Rect.Height < sampleControlResult.Rect.Height / 2, "The popup should not stretch to the height of the screen");

			_app.TapCoordinates(sampleControlResult.Rect.Width / 2, popupResult.Rect.Y - 20);

			_app.WaitForNoElement("PopupBorder");

			TakeScreenshot("Closed");
		}
	}
}
