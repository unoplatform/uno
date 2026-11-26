#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ElementFactoryGetArgs = Microsoft.UI.Xaml.ElementFactoryGetArgs;
using ElementFactoryRecycleArgs = Microsoft.UI.Xaml.ElementFactoryRecycleArgs;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_DataTemplate
{
	[TestMethod]
	[RunsOnUIThread]
	public void When_GetElement_Without_Recycled_Element_Then_Content_Is_Loaded()
	{
		var sut = CreateTemplate();
		var parent = new StackPanel();

		var first = sut.GetElement(new ElementFactoryGetArgs { Parent = parent });
		var second = sut.GetElement(new ElementFactoryGetArgs { Parent = parent });

		Assert.IsInstanceOfType(first, typeof(TextBlock));
		Assert.IsInstanceOfType(second, typeof(TextBlock));
		Assert.AreNotSame(first, second);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Element_Is_Recycled_Then_GetElement_Reuses_It()
	{
		var sut = CreateTemplate();
		var parent = new StackPanel();
		var element = sut.GetElement(new ElementFactoryGetArgs { Parent = parent });

		sut.RecycleElement(new ElementFactoryRecycleArgs { Element = element, Parent = parent });

		Assert.AreSame(element, sut.GetElement(new ElementFactoryGetArgs { Parent = parent }));
		Assert.AreNotSame(element, sut.GetElement(new ElementFactoryGetArgs { Parent = parent }), "A recycled element should be handed out only once.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Elements_Recycled_From_Several_Parents_Then_Same_Parent_Is_Preferred()
	{
		var sut = CreateTemplate();
		var parentA = new StackPanel();
		var parentB = new StackPanel();
		var fromA = sut.GetElement(new ElementFactoryGetArgs { Parent = parentA });
		var fromB = sut.GetElement(new ElementFactoryGetArgs { Parent = parentB });

		sut.RecycleElement(new ElementFactoryRecycleArgs { Element = fromA, Parent = parentA });
		sut.RecycleElement(new ElementFactoryRecycleArgs { Element = fromB, Parent = parentB });

		Assert.AreSame(fromB, sut.GetElement(new ElementFactoryGetArgs { Parent = parentB }));
		Assert.AreSame(fromA, sut.GetElement(new ElementFactoryGetArgs { Parent = parentA }));
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Recycled_Element_Is_Reused_For_Another_Panel_Then_It_Leaves_The_Old_One()
	{
		var sut = CreateTemplate();
		var oldPanel = new StackPanel();
		var newPanel = new StackPanel();
		var element = sut.GetElement(new ElementFactoryGetArgs { Parent = oldPanel });
		oldPanel.Children.Add(element);

		sut.RecycleElement(new ElementFactoryRecycleArgs { Element = element, Parent = oldPanel });
		var reused = sut.GetElement(new ElementFactoryGetArgs { Parent = newPanel });

		Assert.AreSame(element, reused);
		Assert.AreEqual(0, oldPanel.Children.Count, "The reused element should be removed from the panel it was recycled from.");
	}

	private static DataTemplate CreateTemplate()
		=> (DataTemplate)XamlReader.Load("""
			<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
				<TextBlock />
			</DataTemplate>
			""");
}
