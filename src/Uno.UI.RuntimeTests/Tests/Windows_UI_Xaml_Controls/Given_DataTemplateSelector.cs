using System;
using FluentAssertions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_DataTemplateSelector
{
	[TestMethod]
	public void When_Base_Container_Is_Null()
	{
		var sut = new DataTemplateSelector();

		var act = () => sut.SelectTemplate(0, null);

		act.Should().Throw<ArgumentException>();
	}

	[TestMethod]
	public void When_Custom_Container_Is_Null()
	{
		var sut = new CustomTemplateSelector();

		var act = () => sut.SelectTemplate(0, null);

		act.Should().NotThrow();
	}

	internal class CustomTemplateSelector : DataTemplateSelector
	{
		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			// Does not call base - should not throw for null container.
			return null;
		}
	}
}
