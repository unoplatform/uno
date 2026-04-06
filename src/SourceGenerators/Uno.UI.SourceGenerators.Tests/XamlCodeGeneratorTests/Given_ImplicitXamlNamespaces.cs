using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;
using Verify = Uno.UI.SourceGenerators.Tests.Verifiers.XamlSourceGeneratorVerifier;

namespace Uno.UI.SourceGenerators.Tests.Windows_UI_Xaml_Controls.ParserTests;

[TestClass]
public class Given_ImplicitXamlNamespaces
{
	private const string GlobalUri = "http://schemas.microsoft.com/winfx/2006/xaml/presentation/global";

	[TestMethod]
	public async Task When_Ambiguous_Type_In_Global_Namespaces()
	{
		// Two different CLR namespaces both registered to the global URI,
		// each containing a type named "SharedControl".
		var assemblyAttributes = $$"""
			using System.Windows.Markup;
			[assembly: XmlnsDefinition("{{GlobalUri}}", "TestRepro.NsA")]
			[assembly: XmlnsDefinition("{{GlobalUri}}", "TestRepro.NsB")]
			""";

		var controlsSource = """
			using Microsoft.UI.Xaml.Controls;

			namespace TestRepro.NsA
			{
				public class SharedControl : ContentControl { }
			}

			namespace TestRepro.NsB
			{
				public class SharedControl : ContentControl { }
			}
			""";

		var codeBehind = """
			using Microsoft.UI.Xaml.Controls;

			namespace TestRepro
			{
				public sealed partial class MainPage : Page
				{
					public MainPage()
					{
						this.InitializeComponent();
					}
				}
			}
			""";

		var xamlFiles = new[]
		{
			new XamlFile("MainPage.xaml",
				"""
				<Page
					x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

					<SharedControl />
				</Page>

				"""),
		};

		var test = new Verify.Test(xamlFiles)
		{
			TestState =
			{
				Sources = { codeBehind, controlsSource, assemblyAttributes },
			},
			GlobalConfigOverride = new()
			{
				{ "is_global", "true" },
				{ "build_property.MSBuildProjectFullPath", "C:\\Project\\Project.csproj" },
				{ "build_property.RootNamespace", "TestRepro" },
				{ "build_property.UnoForceHotReloadCodeGen", "false" },
				{ "build_property.UnoEnableXamlFuzzyMatching", "false" },
				{ "build_property.UnoEnableImplicitXamlNamespaces", "true" },
			},
		}.AddGeneratedSources();

		test.ExpectedDiagnostics.AddRange(
		[
			DiagnosticResult.CompilerError("UXAML0005")
				.WithArguments("The type 'SharedControl' was found in multiple global XAML namespaces: 'TestRepro.NsA', 'TestRepro.NsB'. Use an explicit xmlns prefix to disambiguate."),
		]);

		await test.RunAsync();
	}
}
