using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Markup;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup;

/// <summary>
/// At runtime, WinUI treats a plain Name attribute like x:Name, including on types whose Name property is get-only.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_XamlReader_Name
{
	private const string Namespaces = "xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'";

	[TestMethod]
	[DataRow("Name")]
	[DataRow("x:Name")]
	public void When_Name_On_Run(string attribute)
	{
		var textBlock = (TextBlock)XamlReader.Load($"<TextBlock {Namespaces}><Run {attribute}='NamedRun' Text='a' /></TextBlock>");

		var run = (Run)textBlock.Inlines[0];
		Assert.AreEqual("NamedRun", run.Name);
	}

	[TestMethod]
	[DataRow("Name")]
	[DataRow("x:Name")]
	public void When_Name_On_Hyperlink(string attribute)
	{
		var textBlock = (TextBlock)XamlReader.Load($"<TextBlock {Namespaces}><Hyperlink {attribute}='NamedLink'>link</Hyperlink></TextBlock>");

		var hyperlink = (Hyperlink)textBlock.Inlines[0];
		Assert.AreEqual("NamedLink", hyperlink.Name);
	}

	[TestMethod]
	public async Task When_Plain_Name_On_VisualState()
	{
		var control = (UserControl)XamlReader.Load($"""
			<UserControl {Namespaces}>
				<Grid>
					<VisualStateManager.VisualStateGroups>
						<VisualStateGroup Name='PlainGroup'>
							<VisualState Name='Wide'>
								<VisualState.Setters>
									<Setter Target='Target.Width' Value='42' />
								</VisualState.Setters>
							</VisualState>
						</VisualStateGroup>
					</VisualStateManager.VisualStateGroups>
					<Border x:Name='Target' Width='1' />
				</Grid>
			</UserControl>
			""");

		try
		{
			await UITestHelper.Load(control, x => x.IsLoaded);

			var root = (Grid)control.Content;
			var group = VisualStateManager.GetVisualStateGroups(root)[0];
			Assert.AreEqual("PlainGroup", group.Name);
			Assert.AreEqual("Wide", group.States[0].Name);

			Assert.IsTrue(VisualStateManager.GoToState(control, "Wide", false));
			Assert.AreEqual(42, ((Border)root.Children[0]).Width);
			Assert.AreEqual("Wide", group.CurrentState?.Name);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
