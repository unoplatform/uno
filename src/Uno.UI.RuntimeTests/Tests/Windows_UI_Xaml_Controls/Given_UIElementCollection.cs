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
	[DataRow(2, 0)]
	[DataRow(2, 2)]
	[DataRow(2, 3)]
	[DataRow(2, 4)]
	public async Task When_Move_To_Valid(int from, int to)
	{
		var panel = await LoadTestPanelAsync();

		panel.Children.Move((uint)from, (uint)to);

		Assert.AreEqual(5, panel.Children.Count);
		for (var i = 0; i < panel.Children.Count; i++)
		{
			if (i == to)
			{
				Assert.IsInstanceOfType(panel.Children[i], typeof(Button));
			}
			else
			{
				Assert.IsInstanceOfType(panel.Children[i], typeof(TextBlock));
			}
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(10, 10)]
	[DataRow(2, 5)]
	[DataRow(5, 2)]
	public async Task When_Move_To_Invalid(int from, int to)
	{
		var panel = await LoadTestPanelAsync();

		var act = () => panel.Children.Move((uint)from, (uint)to);
		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	private async Task<StackPanel> LoadTestPanelAsync()
	{
		var panel = new StackPanel();
		panel.Children.Add(new TextBlock() { Text = "0" });
		panel.Children.Add(new TextBlock() { Text = "1" });
		panel.Children.Add(new Button() { Content = "2" });
		panel.Children.Add(new TextBlock() { Text = "3" });
		panel.Children.Add(new TextBlock() { Text = "4" });

		TestServices.WindowHelper.WindowContent = panel;
		await TestServices.WindowHelper.WaitForLoaded(panel);

		return panel;
	}
}
