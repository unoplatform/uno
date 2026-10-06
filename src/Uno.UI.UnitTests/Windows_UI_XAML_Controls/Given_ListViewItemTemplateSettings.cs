using System.Reflection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_ListViewItemTemplateSettings
{
	[TestMethod]
	public void When_ListViewItem_DragItemsCount_Set_Internally()
	{
		var item = new ListViewItem();

		Assert.AreEqual(0, item.TemplateSettings.DragItemsCount);

		item.TemplateSettings.DragItemsCount = 3;

		Assert.AreEqual(3, item.TemplateSettings.DragItemsCount);
	}

	[TestMethod]
	public void When_GridViewItem_DragItemsCount_Set_Internally()
	{
		var item = new GridViewItem();

		Assert.AreEqual(0, item.TemplateSettings.DragItemsCount);

		item.TemplateSettings.DragItemsCount = 5;

		Assert.AreEqual(5, item.TemplateSettings.DragItemsCount);
	}

	[TestMethod]
	public void When_DragItemsCount_Then_Declared_On_Concrete_Types()
	{
		const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

		Assert.IsNotNull(typeof(ListViewItemTemplateSettings).GetProperty(nameof(ListViewItemTemplateSettings.DragItemsCount), PublicDeclared));
		Assert.IsNotNull(typeof(GridViewItemTemplateSettings).GetProperty(nameof(GridViewItemTemplateSettings.DragItemsCount), PublicDeclared));
		Assert.IsEmpty(typeof(ListViewBaseItemTemplateSettings).GetMembers(PublicDeclared));
	}

	[TestMethod]
	public void When_Item_Then_Derives_From_Hidden_Bases()
	{
		Assert.IsInstanceOfType<ListViewBaseItem>(new ListViewItem());
		Assert.IsInstanceOfType<ListViewBaseItem>(new GridViewItem());
		Assert.IsInstanceOfType<ListViewBaseItemPresenter>(new ListViewItemPresenter());
		Assert.IsInstanceOfType<ListViewBaseItemPresenter>(new GridViewItemPresenter());
		Assert.IsInstanceOfType<ListViewBaseItemTemplateSettings>(new ListViewItem().TemplateSettings);
		Assert.IsInstanceOfType<ListViewBaseItemTemplateSettings>(new GridViewItem().TemplateSettings);
	}
}
