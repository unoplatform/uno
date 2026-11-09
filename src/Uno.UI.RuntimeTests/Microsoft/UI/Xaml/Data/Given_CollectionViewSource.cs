using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data;

// ItemsSource="{Binding Source={StaticResource cvs}}" with the CVS Source set after load: WinUI's binding then pushes the
// new View to ItemsSource, so a Selector syncs to the view's current item (VerifyTabOrderWithGroupedItems*Once* rely on it).
[TestClass]
[RunsOnUIThread]
public class Given_CollectionViewSource
{
	private sealed class Group : List<string>
	{
		public Group(string key, params string[] items) : base(items) => Key = key;

		public string Key { get; }
	}

	private static async Task<CollectionViewSource> LoadBoundToEmptySource(ItemsControl control)
	{
		CollectionViewSource cvs = new();
		control.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = cvs });

		await UITestHelper.Load(control, x => x.IsLoaded);
		return cvs;
	}

	private static void AssertSelectsCurrentItem(Selector selector, CollectionViewSource cvs)
	{
		Assert.IsNotNull(cvs.View);
		Assert.AreEqual(0, cvs.View.CurrentPosition);
		Assert.AreEqual(0, selector.SelectedIndex);
		Assert.AreEqual(cvs.View.CurrentItem, selector.SelectedItem);
	}

	[TestMethod]
	public async Task When_Source_Set_After_Load_Then_ListView_Selects_Current_Item()
	{
		var list = new ListView { Height = 200 };
		var cvs = await LoadBoundToEmptySource(list);

		cvs.Source = new List<string> { "a", "b", "c" };
		await WindowHelper.WaitForIdle();

		AssertSelectsCurrentItem(list, cvs);
	}

	[TestMethod]
	public async Task When_Grouped_Source_Set_After_Load_Then_ListView_Selects_Current_Item()
	{
		var list = new ListView { Height = 200 };
		list.GroupStyle.Add(new GroupStyle());
		var cvs = await LoadBoundToEmptySource(list);

		cvs.IsSourceGrouped = true;
		cvs.Source = new List<Group> { new("1", "11", "12"), new("2", "21", "22") };
		await WindowHelper.WaitForIdle();

		AssertSelectsCurrentItem(list, cvs);
		Assert.AreEqual("11", list.SelectedItem);
	}

	[TestMethod]
	public async Task When_Source_Set_After_Load_Then_ComboBox_Selects_Current_Item()
	{
		var comboBox = new ComboBox();
		var cvs = await LoadBoundToEmptySource(comboBox);

		cvs.Source = new List<string> { "a", "b", "c" };
		await WindowHelper.WaitForIdle();

		AssertSelectsCurrentItem(comboBox, cvs);
	}

	[TestMethod]
	public async Task When_Source_Set_After_Load_Then_ItemsControl_Shows_Items()
	{
		var itemsControl = new ItemsControl();
		var cvs = await LoadBoundToEmptySource(itemsControl);

		cvs.Source = new List<string> { "a", "b", "c" };
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(3, itemsControl.Items.Count);
		Assert.IsNotNull(itemsControl.ContainerFromIndex(2));
	}
}
