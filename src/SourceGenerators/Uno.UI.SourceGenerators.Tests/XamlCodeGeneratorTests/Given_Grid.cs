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
				// Uno.UI.SourceGenerators\Uno.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator\Grid_Uses_Both_Syntaxes_794c1760299b374d12fe38ba3b633206.cs(115,5): error CS1912: Duplicate initialization of member 'ColumnDefinitions'
				DiagnosticResult.CompilerError("CS1912").WithSpan(Path.Combine("Uno.UI.SourceGenerators","Uno.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator","Grid_Uses_Both_Syntaxes_794c1760299b374d12fe38ba3b633206.cs"), 115, 5, 115, 22).WithArguments("ColumnDefinitions"),
				// Uno.UI.SourceGenerators\Uno.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator\Grid_Uses_Both_Syntaxes_794c1760299b374d12fe38ba3b633206.cs(149,5): error CS1912: Duplicate initialization of member 'RowDefinitions'
				DiagnosticResult.CompilerError("CS1912").WithSpan(Path.Combine("Uno.UI.SourceGenerators","Uno.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator","Grid_Uses_Both_Syntaxes_794c1760299b374d12fe38ba3b633206.cs"), 149, 5, 149, 19).WithArguments("RowDefinitions"),
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
