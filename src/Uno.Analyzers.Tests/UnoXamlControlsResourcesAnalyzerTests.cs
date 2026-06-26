using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Uno.Analyzers.Tests.Verifiers;
using System.Collections.Immutable;

namespace Uno.Analyzers.Tests;

using Verify = CSharpCodeFixVerifier<UnoXamlControlsResourcesAnalyzer, EmptyCodeFixProvider>;

[TestClass]
public class UnoXamlControlsResourcesAnalyzerTests
{
#if HAS_UNO_WINUI
	private static readonly ImmutableArray<PackageIdentity> _unoPackage = [new PackageIdentity("Uno.WinUI", "5.2.161")];
#else
	private static readonly ImmutableArray<PackageIdentity> _unoPackage = [new PackageIdentity("Uno.UI", "5.2.161")];
#endif

	private static readonly ReferenceAssemblies _net80WithUno = ReferenceAssemblies.Net.Net80.AddPackages(_unoPackage);

	private const string MinimalCsSource = """
		namespace MyNamespace;
		public class C { }
		""";

	private static readonly string AppXamlWithXcr = """
		<Application
		    x:Class="MyApp.App"
		    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
		    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
		  <Application.Resources>
		    <ResourceDictionary>
		      <ResourceDictionary.MergedDictionaries>
		        <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
		      </ResourceDictionary.MergedDictionaries>
		    </ResourceDictionary>
		  </Application.Resources>
		</Application>
		""";

	private static readonly string AppXamlWithoutXcr = """
		<Application
		    x:Class="MyApp.App"
		    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
		    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
		  <Application.Resources>
		    <ResourceDictionary>
		    </ResourceDictionary>
		  </Application.Resources>
		</Application>
		""";

	private static readonly string PageXaml = """
		<Page
		    x:Class="MyApp.MainPage"
		    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
		    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
		</Page>
		""";

	[TestMethod]
	public async Task AppXamlWithXamlControlsResources_NoDiagnostic()
	{
		var test = new Verify.Test
		{
			TestCode = MinimalCsSource,
			FixedCode = MinimalCsSource,
			ReferenceAssemblies = _net80WithUno,
		};
		test.TestState.AdditionalFiles.Add(("App.xaml", SourceText.From(AppXamlWithXcr)));

		await test.RunAsync();
	}

	[TestMethod]
	public async Task AppXamlMissingXamlControlsResources_Diagnostic()
	{
		var test = new Verify.Test
		{
			TestCode = MinimalCsSource,
			FixedCode = MinimalCsSource,
			ReferenceAssemblies = _net80WithUno,
		};
		test.TestState.AdditionalFiles.Add(("App.xaml", SourceText.From(AppXamlWithoutXcr)));
		test.TestState.ExpectedDiagnostics.Add(
			new DiagnosticResult("Uno0008", Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
				.WithSpan("App.xaml", 1, 2, 1, 2));

		await test.RunAsync();
	}

	[TestMethod]
	public async Task NonApplicationXaml_NoDiagnostic()
	{
		var test = new Verify.Test
		{
			TestCode = MinimalCsSource,
			FixedCode = MinimalCsSource,
			ReferenceAssemblies = _net80WithUno,
		};
		test.TestState.AdditionalFiles.Add(("MainPage.xaml", SourceText.From(PageXaml)));

		await test.RunAsync();
	}

	[TestMethod]
	public async Task NoXamlControlsResourcesTypeInCompilation_NoDiagnostic()
	{
		// Without the Uno package, XamlControlsResources type is not available
		var test = new Verify.Test
		{
			TestCode = MinimalCsSource,
			FixedCode = MinimalCsSource,
			ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
		};
		test.TestState.AdditionalFiles.Add(("App.xaml", SourceText.From(AppXamlWithoutXcr)));

		await test.RunAsync();
	}

	[TestMethod]
	public async Task NonXamlAdditionalFile_NoDiagnostic()
	{
		var test = new Verify.Test
		{
			TestCode = MinimalCsSource,
			FixedCode = MinimalCsSource,
			ReferenceAssemblies = _net80WithUno,
		};
		test.TestState.AdditionalFiles.Add(("Resources.resw", SourceText.From("<root><data name=\"Key\"><value>Value</value></data></root>")));

		await test.RunAsync();
	}
}
