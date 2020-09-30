using System;
using System.Collections.Generic;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;

namespace SamplesApp.UITests.Microsoft_UI_Xaml_Controls.RadioMenuFlyoutItemTests
{
	public partial class Given_RadioMenuFlyoutItem : SampleControlUITestBase
	{
		List<string> Items = new List<string>
		{
			"Red",
			"Orange",
			"Yellow",
			"Green",
			"Blue",
			"Indigo",
			"Violet",
			"Compact",
			"Normal",
			"Expanded"
		};

		[Test]
		[AutoRetry]
		public void BasicTest()
		{
			Run("UITests.Shared.Microsoft_UI_Xaml_Controls.RadioMenuFlyoutItemTests.RadioMenuFlyoutItemPage");
			Console.WriteLine("Verify initial states");
			VerifySelectedItems("Orange", "Compact", "Name");

			InvokeItem("FlyoutButton", "YellowItem");
			VerifySelectedItems("Yellow", "Compact", "Name");

			InvokeItem("FlyoutButton", "ExpandedItem");
			VerifySelectedItems("Yellow", "Expanded", "Name");

			Console.WriteLine("Verify you can't uncheck an item");
			InvokeItem("FlyoutButton", "YellowItem");
			VerifySelectedItems("Yellow", "Expanded", "Name");
		}

		[Test]
		[AutoRetry]
		public void SubMenuTest()
		{
			Run("UITests.Shared.Microsoft_UI_Xaml_Controls.RadioMenuFlyoutItemTests.RadioMenuFlyoutItemPage");
			InvokeSubItem("SubMenuFlyoutButton", "RadioSubMenu", "ArtistNameItem");
			VerifySelectedItems("Orange", "Compact", "ArtistName");

			InvokeItem("SubMenuFlyoutButton", "DateItem");
			VerifySelectedItems("Orange", "Compact", "Date");
		}

		private void InvokeItem(string flyoutButtonName, string itemName)
		{
			Console.WriteLine("Open flyout by clicking " + flyoutButtonName);
			var flyoutButton = _app.Marked(flyoutButtonName);
			flyoutButton.FastTap();

			Console.WriteLine("Invoke item: " + itemName);
			var menuItem = _app.Marked(itemName);
			menuItem.FastTap();
		}

		private void InvokeSubItem(string flyoutButtonName, string menuName, string itemName)
		{
			InvokeItem(flyoutButtonName, menuName);

			Console.WriteLine("Invoke item: " + itemName);
			var menuItem = _app.Marked(itemName);
			menuItem.FastTap();
		}

		public void VerifySelectedItems(string item1, string item2, string item3)
		{
			foreach (string item in Items)
			{
				var itemStateDocumentText = _app.Marked(item + "State").GetText();

				if (item == item1 || item == item2 || item == item3)
				{
					Assert.AreEqual(itemStateDocumentText, "Checked", "Verify " + item + " is checked");
				}
				else
				{
					Assert.AreEqual(itemStateDocumentText, "Unchecked", "Verify " + item + " is unchecked");
				}
			}
		}

	}
}
