#if HAS_UNO
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS | RuntimeTestPlatforms.NativeWasm)]
public class Given_RadioMenuFlyoutItem_UITest
{
	// Migrated from SamplesApp.UITests RadioMenuFlyoutItemTests.Given_RadioMenuFlyoutItem
	// (BasicTest + SubMenuTest). The Selenium original opened the RadioMenuFlyoutItemPage sample
	// and tapped items; here we invoke items directly (the same click code path) and assert
	// IsChecked. Selection is keyed by GroupName in a thread-static map, so flyout/submenu
	// nesting does not affect the outcome.

	[TestMethod]
	public void When_Invoke_Updates_Checked_State_Per_Group()
	{
		// Default group (no GroupName) — mutually exclusive amongst themselves.
		var red = new RadioMenuFlyoutItem { Text = "Red" };
		var orange = new RadioMenuFlyoutItem { Text = "Orange", IsChecked = true };
		var yellow = new RadioMenuFlyoutItem { Text = "Yellow" };

		// "Size" group — independent of the default group.
		var compact = new RadioMenuFlyoutItem { Text = "Compact", GroupName = "Size", IsChecked = true };
		var normal = new RadioMenuFlyoutItem { Text = "Normal", GroupName = "Size" };
		var expanded = new RadioMenuFlyoutItem { Text = "Expanded", GroupName = "Size" };

		Assert.IsTrue(orange.IsChecked);
		Assert.IsTrue(compact.IsChecked);
		Assert.IsFalse(yellow.IsChecked);
		Assert.IsFalse(expanded.IsChecked);

		// Invoking Yellow checks it and unchecks Orange; the Size group is unaffected.
		yellow.Invoke();
		Assert.IsTrue(yellow.IsChecked);
		Assert.IsFalse(orange.IsChecked);
		Assert.IsFalse(red.IsChecked);
		Assert.IsTrue(compact.IsChecked);

		// Invoking Expanded checks it and unchecks Compact; the default group is unaffected.
		expanded.Invoke();
		Assert.IsTrue(expanded.IsChecked);
		Assert.IsFalse(compact.IsChecked);
		Assert.IsFalse(normal.IsChecked);
		Assert.IsTrue(yellow.IsChecked);

		// Invoking an already-checked radio item must not uncheck it.
		yellow.Invoke();
		Assert.IsTrue(yellow.IsChecked);
		Assert.IsTrue(expanded.IsChecked);
	}

	[TestMethod]
	public void When_Invoke_SubMenu_Item_Shares_Group()
	{
		// All items share "SortGroup"; some at the top level, one nested in a submenu.
		var name = new RadioMenuFlyoutItem { Text = "Name", GroupName = "SortGroup", IsChecked = true };
		var date = new RadioMenuFlyoutItem { Text = "Date", GroupName = "SortGroup" };

		var artistName = new RadioMenuFlyoutItem { Text = "ArtistName", GroupName = "SortGroup" };
		var subMenu = new MenuFlyoutSubItem { Text = "Other" };
		subMenu.Items.Add(artistName);

		Assert.IsTrue(name.IsChecked);

		// The submenu item and the top-level items are mutually exclusive (same group).
		artistName.Invoke();
		Assert.IsTrue(artistName.IsChecked);
		Assert.IsFalse(name.IsChecked);

		date.Invoke();
		Assert.IsTrue(date.IsChecked);
		Assert.IsFalse(artistName.IsChecked);
	}
}
#endif
