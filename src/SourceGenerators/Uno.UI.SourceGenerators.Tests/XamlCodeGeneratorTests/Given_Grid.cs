using Uno.UI.SourceGenerators.Tests.Verifiers;
using Microsoft.CodeAnalysis.Testing;

namespace Uno.UI.SourceGenerators.Tests.Windows_UI_Xaml_Controls.GridTests;

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_Grid
{
	[TestMethod]
	public async Task When_Grid_Uses_Both_Syntaxes()
	{
		var test = new TestSetup(xamlFileName: "Grid_Uses_Both_Syntaxes.xaml", subFolder: Path.Combine("SourceGenerators", "Uno.UI.SourceGenerators.Tests", "XamlCodeGeneratorTests", "TestCases"))
		{
			ExpectedDiagnostics =
			{
				DiagnosticResult.CompilerError("XLS0501").WithSpan("C:/Project/0/Grid_Uses_Both_Syntaxes.xaml", 17, 33, 17, 33).WithArguments("The property 'ColumnDefinitions' is set more than once"),
				DiagnosticResult.CompilerError("XLS0501").WithSpan("C:/Project/0/Grid_Uses_Both_Syntaxes.xaml", 24, 30, 24, 30).WithArguments("The property 'RowDefinitions' is set more than once"),
			},
		};

		await Verify.AssertXamlGenerator(test);
	}

	[TestMethod]
	public async Task When_Grid_Uses_Common_Syntax()
	{
		var test = new TestSetup(xamlFileName: "Grid_Uses_Common_Syntax.xaml", subFolder: Path.Combine("Uno.UI.Tests", "Windows_UI_XAML_Controls", "GridTests", "Controls"));
		await Verify.AssertXamlGenerator(test);
	}

	[TestMethod]
	public async Task When_Grid_Uses_New_Assigned_ContentProperty_Syntax()
	{
		var test = new TestSetup(xamlFileName: "Grid_Uses_New_Assigned_ContentProperty_Syntax.xaml", subFolder: Path.Combine("Uno.UI.Tests", "Windows_UI_XAML_Controls", "GridTests", "Controls"));
		await Verify.AssertXamlGenerator(test);
	}

	[TestMethod]
	public async Task When_Grid_Uses_New_Succinct_Syntax()
	{
		var test = new TestSetup(xamlFileName: "Grid_Uses_New_Succinct_Syntax.xaml", subFolder: Path.Combine("Uno.UI.Tests", "Windows_UI_XAML_Controls", "GridTests", "Controls"));
		await Verify.AssertXamlGenerator(test);
	}
}
