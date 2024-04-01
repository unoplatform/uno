using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_UIElementCollection
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Move_To_Middle()
	{
		var panel = new StackPanel();
		panel.Children.Add(new TextBlock());
		panel.Children.Add(new Button());
		panel.Children.Add(new TextBlock());
		panel.Children.Add(new TextBlock());
		panel.Children.Add(new TextBlock());

		TestServices.WindowHelper.WindowContent = panel;
		await TestServices.WindowHelper.WaitForLoaded(panel);

		panel.Children.Move(1, 3);

		Assert.IsInstanceOfType(panel.Children[3], typeof(Button));
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Move_To_End()
	{
		var panel = new StackPanel();
		panel.Children.Add(new TextBlock());
		panel.Children.Add(new Button());
		panel.Children.Add(new TextBlock());
		panel.Children.Add(new TextBlock());
		panel.Children.Add(new TextBlock());

		TestServices.WindowHelper.WindowContent = panel;
		await TestServices.WindowHelper.WaitForLoaded(panel);

		panel.Children.Move(1, 4);

		Assert.IsInstanceOfType(panel.Children[4], typeof(Button));
	}
}
