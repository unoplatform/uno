using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using ElementFactoryGetArgs = Microsoft.UI.Xaml.ElementFactoryGetArgs;
using ElementFactoryRecycleArgs = Microsoft.UI.Xaml.ElementFactoryRecycleArgs;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_DataTemplateSelector
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Base_Container_Is_Null()
	{
		var sut = new DataTemplateSelector();

		var act = () => sut.SelectTemplate(0, null);

		act.Should().Throw<ArgumentException>();
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Custom_Container_Is_Null()
	{
		var sut = new CustomTemplateSelector();

		var act = () => sut.SelectTemplate(0, null);

		act.Should().NotThrow();
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_GetElement_Then_Selected_Template_Is_Used_And_Element_Is_Recycled_Into_It()
	{
		var textTemplate = CreateTemplate("TextBlock");
		var borderTemplate = CreateTemplate("Border");
		var sut = new ByTypeTemplateSelector(textTemplate, borderTemplate);
		var parent = new StackPanel();

		var text = sut.GetElement(new ElementFactoryGetArgs { Data = "a", Parent = parent });
		var border = sut.GetElement(new ElementFactoryGetArgs { Data = 1, Parent = parent });

		Assert.IsInstanceOfType(text, typeof(TextBlock));
		Assert.IsInstanceOfType(border, typeof(Border));

		sut.RecycleElement(new ElementFactoryRecycleArgs { Element = border, Parent = parent });

		Assert.AreSame(border, borderTemplate.GetElement(new ElementFactoryGetArgs { Parent = parent }), "The element should go back to the template that produced it.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Only_Container_Overload_Is_Implemented_Then_GetElement_Uses_It()
	{
		var sut = new ContainerOnlyTemplateSelector(CreateTemplate("TextBlock"));

		var element = sut.GetElement(new ElementFactoryGetArgs { Data = "a", Parent = new StackPanel() });

		Assert.IsInstanceOfType(element, typeof(TextBlock));
	}

	private static DataTemplate CreateTemplate(string root)
		=> (DataTemplate)XamlReader.Load($"""
			<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
				<{root} />
			</DataTemplate>
			""");

	internal class CustomTemplateSelector : DataTemplateSelector
	{
		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			// Does not call base - should not throw for null container.
			return null;
		}
	}

	private sealed partial class ByTypeTemplateSelector(DataTemplate stringTemplate, DataTemplate otherTemplate) : DataTemplateSelector
	{
		protected override DataTemplate SelectTemplateCore(object item) => item is string ? stringTemplate : otherTemplate;
	}

	private sealed partial class ContainerOnlyTemplateSelector(DataTemplate template) : DataTemplateSelector
	{
		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => template;
	}
}
