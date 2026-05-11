using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_RadioMenuFlyoutItem
{
	[TestMethod]
	public async Task When_Check_Sequence()
	{
		var item1 = new Microsoft.UI.Xaml.Controls.RadioMenuFlyoutItem { GroupName = "group1", IsChecked = true };
		var item2 = new Microsoft.UI.Xaml.Controls.RadioMenuFlyoutItem { GroupName = "group1" };
		var item3 = new Microsoft.UI.Xaml.Controls.RadioMenuFlyoutItem { GroupName = "group1" };

		var menu = new Microsoft.UI.Xaml.Controls.MenuFlyout();
		menu.Items.Add(item1);
		menu.Items.Add(item2);
		menu.Items.Add(item3);

		var button = new Microsoft.UI.Xaml.Controls.Button
		{
			Content = "Open Menu"
		};
		FlyoutBase.SetAttachedFlyout(button, menu);
		TestServices.WindowHelper.WindowContent = button;
		await TestServices.WindowHelper.WaitForLoaded(button);
		FlyoutBase.ShowAttachedFlyout(button);
		await TestServices.WindowHelper.WaitForLoaded(item1);
		item1.IsChecked = true;
		Assert.IsTrue(item1.IsChecked);
		Assert.IsFalse(item2.IsChecked);
		Assert.IsFalse(item3.IsChecked);
		item2.IsChecked = true;
		Assert.IsFalse(item1.IsChecked);
		Assert.IsTrue(item2.IsChecked);
		Assert.IsFalse(item3.IsChecked);
		item3.IsChecked = true;
		Assert.IsFalse(item1.IsChecked);
		Assert.IsFalse(item2.IsChecked);
		Assert.IsTrue(item3.IsChecked);
		item1.IsChecked = true;
		Assert.IsTrue(item1.IsChecked);
		item1.IsChecked = false;
		Assert.IsFalse(item1.IsChecked);
		item1.IsChecked = true;
		Assert.IsTrue(item1.IsChecked);
	}

	[TestMethod]
	public async Task When_IsChecked_Set_Before_GroupName_In_Xaml()
	{
		// Reproduces https://github.com/unoplatform/uno/issues/19090.
		// When IsChecked="True" appears before GroupName in XAML, the item must still
		// be registered under the correct group so subsequent selections deselect it.
		var menu = (Microsoft.UI.Xaml.Controls.MenuFlyout)XamlReader.Load(
			"""
			<MenuFlyout xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
				<RadioMenuFlyoutItem Text="Small" GroupName="SizeGroup" />
				<RadioMenuFlyoutItem Text="Medium" IsChecked="True" GroupName="SizeGroup" />
				<RadioMenuFlyoutItem Text="Large" GroupName="SizeGroup" />
			</MenuFlyout>
			""");

		var small = (Microsoft.UI.Xaml.Controls.RadioMenuFlyoutItem)menu.Items[0];
		var medium = (Microsoft.UI.Xaml.Controls.RadioMenuFlyoutItem)menu.Items[1];
		var large = (Microsoft.UI.Xaml.Controls.RadioMenuFlyoutItem)menu.Items[2];

		var button = new Microsoft.UI.Xaml.Controls.Button { Content = "Open Menu" };
		FlyoutBase.SetAttachedFlyout(button, menu);
		TestServices.WindowHelper.WindowContent = button;
		await TestServices.WindowHelper.WaitForLoaded(button);
		FlyoutBase.ShowAttachedFlyout(button);
		await TestServices.WindowHelper.WaitForLoaded(medium);

		Assert.IsFalse(small.IsChecked);
		Assert.IsTrue(medium.IsChecked);
		Assert.IsFalse(large.IsChecked);

		small.IsChecked = true;
		Assert.IsTrue(small.IsChecked);
		Assert.IsFalse(medium.IsChecked);
		Assert.IsFalse(large.IsChecked);

		large.IsChecked = true;
		Assert.IsFalse(small.IsChecked);
		Assert.IsFalse(medium.IsChecked);
		Assert.IsTrue(large.IsChecked);
	}
}
