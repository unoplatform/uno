using System;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Toolkit.DevTools.Input;
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
		var items = new[] { CreateItem("0"), CreateItem("1"), CreateItem("2") };
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

	// 6.8 has no default ListBoxItem style, so only the item's content is hit-testable: stretch it over the row.
	private static ListBoxItem CreateItem(string text) => new()
	{
		HorizontalContentAlignment = HorizontalAlignment.Stretch,
		Content = new Border { Background = new SolidColorBrush(Colors.Transparent), Child = new TextBlock { Text = text } },
	};
}
