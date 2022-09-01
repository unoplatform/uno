namespace Uno.UI.SourceGenerators.Tests.Windows_UI_Xaml_Controls.GridTests;

using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using static XamlCodeGeneratorHelper;

[TestClass]
public class Given_Grid
{
	[TestMethod]
	public async Task When_Grid_Uses_Both_Syntaxes()
	{
		var diagnostics = await RunXamlCodeGeneratorForFileAsync(
			xamlFileName: "Grid_Uses_Both_Syntaxes.xaml",
			subFolder: Path.Combine("SourceGenerators", "Uno.UI.SourceGenerators.net6.Tests", "Windows_UI_Xaml_Controls", "GridTests", "Controls"));

		diagnostics.AssertDiagnostics(
			new DiagnosticResult("CS1912", DiagnosticSeverity.Error).WithMessage(@"Duplicate initialization of member 'ColumnDefinitions'"),
			new DiagnosticResult("CS1912", DiagnosticSeverity.Error).WithMessage(@"Duplicate initialization of member 'RowDefinitions'"));
	}

	[TestMethod]
	public async Task When_Grid_Uses_Common_Syntax()
	{
		var diagnostics = await RunXamlCodeGeneratorForFileAsync(
			xamlFileName: "Grid_Uses_Common_Syntax.xaml",
			subFolder: Path.Combine("Uno.UI.Tests", "Windows_UI_XAML_Controls", "GridTests", "Controls"));

		diagnostics.AssertDiagnostics();
	}

	[TestMethod]
	public async Task When_Grid_Uses_New_Assigned_ContentProperty_Syntax()
	{
		var diagnostics = await RunXamlCodeGeneratorForFileAsync(
			xamlFileName: "Grid_Uses_New_Assigned_ContentProperty_Syntax.xaml",
			subFolder: Path.Combine("Uno.UI.Tests", "Windows_UI_XAML_Controls", "GridTests", "Controls"));

		diagnostics.AssertDiagnostics();
	}

	[TestMethod]
	public async Task When_Grid_Uses_New_Succinct_Syntax()
	{
		var diagnostics = await RunXamlCodeGeneratorForFileAsync(
			xamlFileName: "Grid_Uses_New_Succinct_Syntax.xaml",
			subFolder: Path.Combine("Uno.UI.Tests", "Windows_UI_XAML_Controls", "GridTests", "Controls"));

		diagnostics.AssertDiagnostics();
	}
}
