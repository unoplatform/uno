using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_RadioButtons_UITest
{
	// Migrated from SamplesApp.UITests RadioButtonsTests/Given_RadioButtons.SelectionTest.
	// Original was Android+Browser only (flaky on iOS #9080); reconstructed inline for Skia,
	// enabled on all platforms since the Selenium flakiness does not apply to a runtime test.
	[TestMethod]
	[DataRow(false, DisplayName = "Items")]
	[DataRow(true, DisplayName = "ItemsSource")]
	public async Task When_Select_By_Index_And_Item(bool useItemsSource)
	{
		const int count = 10;
		var items = new ObservableCollection<RadioButton>();
		for (int i = 0; i < count; i++)
		{
			items.Add(new RadioButton { Content = $"{i} Radio Button" });
		}

		var sut = new RadioButtons();
		if (useItemsSource)
		{
			sut.ItemsSource = items;
		}
		else
		{
			foreach (var item in items)
			{
				sut.Items.Add(item);
			}
		}

		try
		{
			await UITestHelper.Load(sut);

			// Select by index -> the container at that index becomes checked.
			sut.SelectedIndex = 1;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(1, sut.SelectedIndex);
			Assert.IsTrue(items[1].IsChecked == true, "Item 1 should be checked after SelectedIndex = 1");

			// Select by item -> the new item is checked and the previous one is cleared.
			sut.SelectedItem = items[3];
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(3, sut.SelectedIndex);
			Assert.IsTrue(items[3].IsChecked == true, "Item 3 should be checked after SelectedItem = items[3]");
			Assert.IsFalse(items[1].IsChecked == true, "Item 1 should be unchecked after selecting item 3");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	// Migrated from SamplesApp.UITests RadioButtonsTests/Given_RadioButtons.SelectionOnLoad.
	// SelectedIndex set before the control loads must still check the corresponding item.
	[TestMethod]
	public async Task When_SelectedIndex_Set_Before_Load()
	{
		var lightThemeRadio = new RadioButton { Content = "Light Theme" };
		var darkThemeRadio = new RadioButton { Content = "Dark Theme" };
		var systemThemeRadio = new RadioButton { Content = "System Default Theme" };

		var sut = new RadioButtons();
		sut.Items.Add(lightThemeRadio);
		sut.Items.Add(darkThemeRadio);
		sut.Items.Add(systemThemeRadio);
		sut.SelectedIndex = 0;

		try
		{
			await UITestHelper.Load(sut);
			Assert.IsTrue(lightThemeRadio.IsChecked == true, "First item should be checked after load");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
