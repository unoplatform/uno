using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;

namespace SamplesApp.UITests.Microsoft_UI_Xaml_Controls.NumberBoxTests
{
	public class Given_RadioButtons : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void SelectionTest()
		{
			Run("UITests.Microsoft_UI_Xaml_Controls.RadioButtonsTests.RadioButtonsPage");

			SetItemType(RadioButtonsSourceType.RadioButton);

			foreach (RadioButtonsSourceLocation location in Enum.GetValues(typeof(RadioButtonsSourceLocation)))
			{
				SetSource(location);

				SelectByIndex(1);
				VerifySelectedIndex(1);
				var item1 = _app.Marked("Radio Button 1");
				Assert.IsTrue(item1.GetDependencyPropertyValue<string>("IsChecked") == "True");

				SelectByItem(3);
				VerifySelectedIndex(3);
				var item3 = _app.Marked("Radio Button 3");
				Assert.IsTrue(item3.GetDependencyPropertyValue<string>("IsChecked") == "True");
				Assert.IsFalse(item1.GetDependencyPropertyValue<string>("IsChecked") == "True");
			}
		}

		private void SelectByItem(int v)
		{
			SetIndexToSelect(v);

			var selectByIndexButton = _app.Marked("SelectByItemButton");
			selectByIndexButton.FastTap();
		}

		private void VerifySelectedIndex(int v)
		{
			var selectedIndexTextBlock = _app.Marked("SelectedIndexTextBlock");
			//var v2 = selectedIndexTextBlock.GetDependencyPropertyValue("Inlines");
			_app.WaitForText(selectedIndexTextBlock, v.ToString(CultureInfo.InvariantCulture));
		}
		private void SelectByIndex(int v)
		{
			SetIndexToSelect(v);

			var selectByIndexButton = _app.Marked("SelectByIndexButton");
			selectByIndexButton.FastTap();
		}

		private void SetIndexToSelect(int v)
		{
			var indexToSelectTextBlock = _app.Marked("IndexToSelectTextBlock");
			indexToSelectTextBlock.SetDependencyPropertyValue("Text", v.ToString(CultureInfo.InvariantCulture));
		}

		private void SetSource(RadioButtonsSourceLocation location)
		{
			var sourceComboBox = _app.Marked("SourceComboBox");

			sourceComboBox.SetDependencyPropertyValue("SelectedIndex", "1");
			sourceComboBox.SetDependencyPropertyValue("SelectedIndex", "0");
			sourceComboBox.SetDependencyPropertyValue("SelectedIndex", "-1");

			switch (location)
			{
				case RadioButtonsSourceLocation.Items:
					sourceComboBox.SetDependencyPropertyValue("SelectedIndex", "0");
					break;
				case RadioButtonsSourceLocation.ItemSource:
					sourceComboBox.SetDependencyPropertyValue("SelectedIndex", "1");
					break;
			}
		}
		private void SetItemType(RadioButtonsSourceType type)
		{
			var itemTypeComboBox = _app.Marked("ItemTypeComboBox");

			switch (type)
			{
				case RadioButtonsSourceType.String:
					itemTypeComboBox.SetDependencyPropertyValue("SelectedIndex", "0");
					// elements.GetItemTypeComboBox().SelectItemByName("StringsComboBoxItem");
					break;
				case RadioButtonsSourceType.RadioButton:
					itemTypeComboBox.SetDependencyPropertyValue("SelectedIndex", "1");
					// elements.GetItemTypeComboBox().SelectItemByName("RadioButtonElementsComboBoxItem");
					break;
			}
		}

		public enum RadioButtonsSourceType
		{
			String,
			RadioButton
		}

		public enum RadioButtonsSourceLocation
		{
			Items,
			ItemSource
		}
	}
}
