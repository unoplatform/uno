using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;
using Query = System.Func<Uno.UITest.IAppQuery, Uno.UITest.IAppQuery>;

namespace SamplesApp.UITests.Microsoft_UI_Xaml_Controls.RadioButtonsTests
{
	public partial class Given_RadioButtons : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.Browser)] // Flaky on iOS #9080
		public void SelectionTest()
		{
			Run("UITests.Microsoft_UI_Xaml_Controls.RadioButtonsTests.RadioButtonsPage");

			SetItemType(RadioButtonsSourceType.RadioButton);

			foreach (var location in Enum.GetValues<RadioButtonsSourceLocation>())
			{
				SetSource(location);

				SelectByIndex(1);
				VerifySelectedIndex(1);
				var item1 = QueryAll("Radio Button 1");
				Assert.IsTrue(item1.GetDependencyPropertyValue<string>("IsChecked") == "True");

				SelectByItem(3);
				VerifySelectedIndex(3);
				var item3 = QueryAll("Radio Button 3");
				Assert.IsTrue(item3.GetDependencyPropertyValue<string>("IsChecked") == "True");
				Assert.IsFalse(item1.GetDependencyPropertyValue<string>("IsChecked") == "True");
			}
		}

		[Test]
		[AutoRetry]
		public void SelectionOnLoad()
		{
			Run("UITests.Microsoft_UI_Xaml_Controls.RadioButtonsTests.RadioButtonsInitialLoadSelected");

			var rad = QueryAll("LightThemeRadio");

			Assert.IsTrue(rad.GetDependencyPropertyValue<string>("IsChecked") == "True");
		}

		private void SelectByItem(int v)
		{
			SetIndexToSelect(v);

			var selectByIndexButton = QueryAll("SelectByItemButton");
			selectByIndexButton.FastTap();
		}

		private void VerifySelectedIndex(int v)
		{
			var selectedIndexTextBlock = QueryAll("SelectedIndexTextBlock");
			_app.WaitForText(selectedIndexTextBlock, v.ToString(CultureInfo.InvariantCulture));
		}
		private void SelectByIndex(int v)
		{
			SetIndexToSelect(v);

			var selectByIndexButton = QueryAll("SelectByIndexButton");
			selectByIndexButton.FastTap();
		}

		private void SetIndexToSelect(int v)
		{
			var indexToSelectTextBlock = _app.Marked("IndexToSelectTextBlock");
			indexToSelectTextBlock.SetDependencyPropertyValue("Text", v.ToString(CultureInfo.InvariantCulture));
		}

		private void SetSource(RadioButtonsSourceLocation location)
		{
			var sourceComboBox = QueryAll("SourceComboBox");

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
			var itemTypeComboBox = QueryAll("ItemTypeComboBox");

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

		private QueryEx QueryAll(string name)
		{
			IAppQuery AllQuery(IAppQuery query)
				// TODO: .All() is not yet supported for wasm.
				=> AppInitializer.GetLocalPlatform() == Platform.Browser ? query : query.All();

			Query allQuery = q => AllQuery(q).Marked(name);
			return new QueryEx(allQuery);
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
