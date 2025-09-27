using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
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
}
