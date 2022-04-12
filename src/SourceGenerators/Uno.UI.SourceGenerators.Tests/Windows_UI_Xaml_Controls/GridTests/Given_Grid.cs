#if NET6_0_OR_GREATER
namespace Uno.UI.SourceGenerators.Tests.Windows_UI_Xaml_Controls.GridTests;

using static XamlCodeGeneratorHelper;

[TestClass]
public class Given_Grid
{
	[TestMethod]
	public async Task When_Grid_Uses_Both_Syntaxes()
	{
		var diagnostics = await RunXamlCodeGeneratorForFileAsync(
			xamlFileName: "Grid_Uses_Both_Syntaxes.xaml",
			subFolder: Path.Combine("SourceGenerators", "Uno.UI.SourceGenerators.Tests", "Windows_UI_Xaml_Controls", "GridTests", "Controls"));

		diagnostics.Should().NotBeEmpty();
	}

	[TestMethod]
	public async Task When_Grid_Uses_Common_Syntax()
	{
		var diagnostics = await RunXamlCodeGeneratorForFileAsync(
			xamlFileName: "Grid_Uses_Common_Syntax.xaml",
			subFolder: Path.Combine("Uno.UI.Tests", "Windows_UI_XAML_Controls", "GridTests", "Controls"));

		diagnostics.Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_Grid_Uses_New_Assigned_ContentProperty_Syntax()
	{
		var diagnostics = await RunXamlCodeGeneratorForFileAsync(
			xamlFileName: "Grid_Uses_New_Assigned_ContentProperty_Syntax.xaml",
			subFolder: Path.Combine("Uno.UI.Tests", "Windows_UI_XAML_Controls", "GridTests", "Controls"));

		diagnostics.Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_Grid_Uses_New_Succinct_Syntax()
	{
		var diagnostics = await RunXamlCodeGeneratorForFileAsync(
			xamlFileName: "Grid_Uses_New_Succinct_Syntax.xaml",
			subFolder: Path.Combine("Uno.UI.Tests", "Windows_UI_XAML_Controls", "GridTests", "Controls"));

		diagnostics.Should().BeEmpty();
	}
}
#endif
