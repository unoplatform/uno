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
			_app.Tap(button);

			var first = _app.FindWithin("_tb1", presenter);
			Assert.AreEqual("Item 1", first.GetDependencyPropertyValue<string>("Text"));

			_app.Tap(comboBox);
			_app.TapCoordinates(300, 100);
			first = _app.FindWithin("_tb1", presenter);
			Assert.AreEqual("Item 1", first.GetDependencyPropertyValue<string>("Text"));

			_app.Tap(button);
			var second = _app.FindWithin("_tb2", presenter);
			Assert.AreEqual("Item 2", second.GetDependencyPropertyValue<string>("Text"));

			// Close the combo box to not pollute other tests ...
			_app.TapCoordinates(10, 10);

			Configuration.AttemptToFindTargetBeforeScrolling = scrollingInitial;
		}
	}
}
