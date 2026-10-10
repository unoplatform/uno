using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup;

[TestClass]
[RunsOnUIThread]
public class Given_FindName_NonFrameworkElement
{
	private const string Ns = "xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'";

	[TestMethod]
	public void When_Border_XName_Control()
	{
		var grid = (Grid)XamlReader.Load($"<Grid {Ns}><Border x:Name='B'/></Grid>");
		Assert.IsInstanceOfType(grid.FindName("B"), typeof(Border));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/21129")]
	[DataRow("x:Name")]
	[DataRow("Name")]
	public void When_Run(string attr)
	{
		var tb = (TextBlock)XamlReader.Load($"<TextBlock {Ns}><Run {attr}='R' Text='a'/></TextBlock>");
		Assert.IsInstanceOfType(tb.FindName("R"), typeof(Run));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/21129")]
	[DataRow("x:Name")]
	[DataRow("Name")]
	public void When_Hyperlink(string attr)
	{
		var tb = (TextBlock)XamlReader.Load($"<TextBlock {Ns}><Hyperlink {attr}='L'>x</Hyperlink></TextBlock>");
		Assert.IsInstanceOfType(tb.FindName("L"), typeof(Hyperlink));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/21129")]
	public void When_Brush_In_Resources()
	{
		var grid = (Grid)XamlReader.Load($"<Grid {Ns}><Grid.Resources><SolidColorBrush x:Key='k' x:Name='SB' Color='Red'/></Grid.Resources></Grid>");
		Assert.AreSame(grid.Resources["k"], grid.FindName("SB"));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/21129")]
	public async Task When_Run_InLiveTree()
	{
		var grid = (Grid)XamlReader.Load($"<Grid {Ns}><TextBlock><Run x:Name='R2' Text='hello'/></TextBlock></Grid>");
		await UITestHelper.Load(grid);
		Assert.IsInstanceOfType(grid.FindName("R2"), typeof(Run));
	}

	[TestMethod]
	public void When_DataTemplate_Content_Then_Its_Names_Resolve_From_Its_Root()
	{
		var grid = (Grid)XamlReader.Load(
			$"<Grid {Ns}><Grid.Resources><DataTemplate x:Key='t'><TextBlock><Run x:Name='InnerRun' Text='a'/></TextBlock></DataTemplate></Grid.Resources></Grid>");

		var root = (TextBlock)((DataTemplate)grid.Resources["t"]).LoadContent();

		Assert.IsInstanceOfType(root.FindName("InnerRun"), typeof(Run));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/21129")]
	public async Task When_Live_ControlTemplate_Then_Names_Do_Not_Cross_Scopes()
	{
		var (grid, probe) = await LoadTemplatePartProbe();
		var templateRoot = (FrameworkElement)VisualTreeHelper.GetChild(probe, 0);

		Assert.IsInstanceOfType(grid.FindName("OuterRun"), typeof(Run));
		Assert.IsInstanceOfType(templateRoot.FindName("TemplateRun"), typeof(Run));
		Assert.IsNull(grid.FindName("TemplateRun"), "A template's names must not leak into the outer namescope.");
		Assert.IsNull(templateRoot.FindName("OuterRun"), "The outer namescope must not be visible from inside a template.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/21129")]
	public async Task When_Template_Part_Missing_Then_GetTemplateChild_Ignores_Outer_NameScope()
	{
		var (_, probe) = await LoadTemplatePartProbe();

		Assert.IsInstanceOfType(probe.GetPart("TemplateRun"), typeof(Run));
		Assert.IsNull(probe.GetPart("HeaderContentPresenter"), "WinUI's GetTemplateChild only looks in the template namescope.");
		Assert.IsNull(probe.GetPart("OuterRun"));
	}

	private static async Task<(Grid Grid, TemplatePartProbe Probe)> LoadTemplatePartProbe()
	{
		var grid = (Grid)XamlReader.Load(
			$"<Grid {Ns}><ContentPresenter x:Name='HeaderContentPresenter'/><TextBlock><Run x:Name='OuterRun' Text='o'/></TextBlock></Grid>");
		var probe = new TemplatePartProbe
		{
			Width = 50,
			Height = 50,
			Template = (ControlTemplate)XamlReader.Load(
				$"<ControlTemplate {Ns}><Grid><TextBlock><Run x:Name='TemplateRun' Text='t'/></TextBlock></Grid></ControlTemplate>"),
		};
		grid.Children.Add(probe);

		await UITestHelper.Load(grid);
		probe.ApplyTemplate();

		return (grid, probe);
	}

	private sealed class TemplatePartProbe : Control
	{
		public DependencyObject GetPart(string name) => GetTemplateChild(name);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/21129")]
	public async Task When_Compiled_TextElements()
	{
		var page = new FindName_NonFrameworkElement_Page();
		await UITestHelper.Load(page);

		Assert.AreSame(page.CompiledRunElement, page.FindName("CompiledRun"));
		Assert.AreSame(page.CompiledLinkElement, page.FindName("CompiledLink"));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/21129")]
	public async Task When_Compiled_Transform()
	{
		var page = new FindName_NonFrameworkElement_Page();
		await UITestHelper.Load(page);

		Assert.AreSame(page.CompiledTransformElement, page.FindName("CompiledTransform"));
	}

	[TestMethod]
	public async Task When_Compiled_MenuFlyout()
	{
		var page = new FindName_NonFrameworkElement_Page();
		await UITestHelper.Load(page);

		Assert.AreSame(page.CompiledMenuElement, page.FindName("CompiledMenu"));
	}

	[TestMethod]
	public void When_XName_On_NonFrameworkElement_Then_Name_Property_Is_Set()
	{
		var grid = (Grid)XamlReader.Load($"<Grid {Ns}><Grid.Resources><Storyboard x:Key='k' x:Name='SB'/></Grid.Resources></Grid>");

		var storyboard = (Storyboard)grid.Resources["k"];

		Assert.AreEqual("SB", storyboard.GetValue(FrameworkElement.NameProperty));
	}

	[TestMethod]
	public void When_Compiled_XName_On_NonFrameworkElement_Then_Name_Property_Is_Set()
	{
		var page = new FindName_NonFrameworkElement_Page();

		Assert.AreEqual("CompiledTransform", page.CompiledTransformElement.GetValue(FrameworkElement.NameProperty));
		Assert.AreEqual("CompiledMenu", page.CompiledMenuElement.GetValue(FrameworkElement.NameProperty));
	}

	[TestMethod]
	public void When_NameProperty_Set_On_DependencyObject_Then_No_AutomationId_Side_Effect()
	{
		var brush = new SolidColorBrush();

		brush.SetValue(FrameworkElement.NameProperty, "Named");

		Assert.AreEqual("Named", brush.GetValue(FrameworkElement.NameProperty));
		Assert.AreEqual("", AutomationProperties.GetAutomationId(brush));
	}
}
