using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_Pivot_UITest
{
	// Migrated from SamplesApp.UITests UnoSamples_Pivot_Tests, backed by the
	// Pivot_CustomContent_Automated sample: a Pivot populated with non-PivotItem
	// objects, whose SelectedItem.Title/Content are surfaced through bindings.

	[TestMethod]
	public async Task When_Non_PivotItem_Items()
	{
		var sut = BuildSut();
		try
		{
			await UITestHelper.Load(sut.Root);

			Assert.AreEqual("item 1", sut.Title.Text);
			Assert.AreEqual("My Item 1 Content", sut.Content.Text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Non_PivotItemChange_Validation()
	{
		var sut = BuildSut();
		try
		{
			await UITestHelper.Load(sut.Root);

			Assert.AreEqual("item 1", sut.Title.Text);
			Assert.AreEqual("My Item 1 Content", sut.Content.Text);

			// Select the second item and assert the bound values follow.
			sut.Pivot.SelectedIndex = 1;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("item 2", sut.Title.Text);
			Assert.AreEqual("My Item 2 Content", sut.Content.Text);

			// Select the first item again and assert.
			sut.Pivot.SelectedIndex = 0;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("item 1", sut.Title.Text);
			Assert.AreEqual("My Item 1 Content", sut.Content.Text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static (Grid Root, TextBlock Title, TextBlock Content, Pivot Pivot) BuildSut()
	{
		var pivot = new Pivot
		{
			HeaderTemplate = CreateTemplate("<TextBlock Text=\"{Binding Title}\" />"),
			ItemTemplate = CreateTemplate("<ContentControl Content=\"{Binding Content}\" />"),
		};
		pivot.Items.Add(new MyCustomPivotItem { Title = "item 1", Content = "My Item 1 Content" });
		pivot.Items.Add(new MyCustomPivotItem { Title = "item 2", Content = "My Item 2 Content" });

		var title = new TextBlock();
		title.SetBinding(TextBlock.TextProperty, new Binding { Source = pivot, Path = new PropertyPath("SelectedItem.Title") });

		var content = new TextBlock();
		content.SetBinding(TextBlock.TextProperty, new Binding { Source = pivot, Path = new PropertyPath("SelectedItem.Content") });

		var header = new StackPanel { Children = { title, content } };
		Grid.SetRow(header, 0);
		Grid.SetRow(pivot, 1);

		var root = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
			},
			Children = { header, pivot },
		};

		return (root, title, content, pivot);
	}

	private static DataTemplate CreateTemplate(string inner)
		=> (DataTemplate)XamlReader.Load(
			$"<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">{inner}</DataTemplate>");

	public class MyCustomPivotItem
	{
		public string Title { get; set; }
		public string Content { get; set; }
	}
}
