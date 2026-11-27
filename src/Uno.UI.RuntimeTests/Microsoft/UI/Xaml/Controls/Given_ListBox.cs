using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Uno.UI.DevTools.Input;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_ListBox
{
	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_Touch_Hold_Item_Then_Selected()
	{
		var items = new[] { new ListBoxItem { Content = "0" }, new ListBoxItem { Content = "1" }, new ListBoxItem { Content = "2" } };
		var list = new ListBox
		{
			Width = 200,
			Height = 200,
			ItemsPanel = (ItemsPanelTemplate)XamlReader.Load(
				"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><StackPanel /></ItemsPanelTemplate>"),
		};
		foreach (var item in items)
		{
			list.Items.Add(item);
		}

		try
		{
			await UITestHelper.Load(list);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			// Unlike a ListViewItem, WinUI's ListBoxItem selects on the unhandled RightTapped which follows a touch hold.
			await finger.Hold(items[1].GetAbsoluteCenter());
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, list.SelectedIndex);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_SelectedIndex_Changed_Then_Container_IsSelected_Follows()
	{
		var items = new[] { new ListBoxItem { Content = "0" }, new ListBoxItem { Content = "1" }, new ListBoxItem { Content = "2" } };
		var list = new ListBox
		{
			Width = 200,
			Height = 200,
			ItemsPanel = (ItemsPanelTemplate)XamlReader.Load(
				"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><StackPanel /></ItemsPanelTemplate>"),
		};
		foreach (var item in items)
		{
			list.Items.Add(item);
		}
		list.SelectedIndex = 1;

		try
		{
			await UITestHelper.Load(list);
			Assert.IsTrue(items[1].IsSelected);

			list.SelectedIndex = 2;

			Assert.IsFalse(items[1].IsSelected, "The previously selected container must be deselected.");
			Assert.IsTrue(items[2].IsSelected, "The newly selected container must be selected.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
