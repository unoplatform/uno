#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_RadioButtons
{
	[TestMethod]
	public async Task When_ItemTemplate_Is_Custom_ElementFactory_Then_It_Creates_The_Items()
	{
		var factory = new RadioButtonFactory();
		var sut = new RadioButtons { ItemsSource = new[] { "one", "two" }, ItemTemplate = factory };

		await UITestHelper.Load(sut);

		CollectionAssert.AreEquivalent(new[] { "one", "two" }, factory.RequestedData.ToArray());
		Assert.AreEqual("factory", (sut.ContainerFromIndex(0) as RadioButton)?.Tag);
		Assert.AreEqual("factory", (sut.ContainerFromIndex(1) as RadioButton)?.Tag);
	}

	[TestMethod]
	public async Task When_ItemTemplate_Is_DataTemplateSelector_Then_The_Selected_Template_Is_Used()
	{
		var template = (DataTemplate)XamlReader.Load("""
			<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
				<RadioButton Content="{Binding}" Tag="selected" />
			</DataTemplate>
			""");
		var sut = new RadioButtons { ItemsSource = new[] { "one", "two" }, ItemTemplate = new SingleTemplateSelector(template) };

		await UITestHelper.Load(sut);

		var first = sut.ContainerFromIndex(0) as RadioButton;
		Assert.AreEqual("selected", first?.Tag);
		Assert.AreEqual("one", first?.Content);
	}

	private sealed partial class RadioButtonFactory : IElementFactory
	{
		public List<object?> RequestedData { get; } = new();

		public UIElement GetElement(global::Microsoft.UI.Xaml.ElementFactoryGetArgs args)
		{
			RequestedData.Add(args.Data);
			return new RadioButton { Content = args.Data, Tag = "factory" };
		}

		public void RecycleElement(global::Microsoft.UI.Xaml.ElementFactoryRecycleArgs args)
		{
		}
	}

	private sealed partial class SingleTemplateSelector(DataTemplate template) : DataTemplateSelector
	{
		protected override DataTemplate SelectTemplateCore(object item) => template;
	}
}
