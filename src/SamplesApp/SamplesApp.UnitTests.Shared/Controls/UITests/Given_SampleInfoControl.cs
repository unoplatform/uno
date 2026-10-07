#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests;
using Uno.UI.Samples.Controls;

namespace SamplesApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_SampleInfoControl
{
	[TestMethod]
	public void When_Copy_Parameter_Is_Not_Text_Nothing_Throws()
	{
		SampleInfoControl control = new();

		foreach (object? parameter in new object?[] { null, string.Empty, 42 })
		{
			control.CopyContentClick(new Button { CommandParameter = parameter }, new RoutedEventArgs());
		}

		control.CopyContentClick(new Border(), new RoutedEventArgs());
	}
}
