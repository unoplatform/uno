using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests.CSharpExpressions;

using Verify = XamlSourceGeneratorVerifier;

/// <summary>
/// Regression suite for spec FR-013a / FR-016: when <c>UnoXamlCSharpExpressionsEnabled</c>
/// is off (the default), generator output MUST be byte-identical to pre-feature behavior
/// for every existing XAML input.
/// </summary>
/// <remarks>
/// Phase 2 scaffolds the regression harness against a minimal standard-XAML baseline.
/// Phase 9 (T091) expands the golden corpus to cover every existing markup-extension case
/// in <c>src/Uno.UI.Tests</c>.
/// </remarks>
[TestClass]
public class Given_OptInBehavior
{
	[TestMethod]
	public async Task When_FeatureDisabled_StandardXaml_ProducesNoNewDiagnostics()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
			<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<TextBlock Text="Hello, world!" />
			</Page>
			""");

		var test = new Verify.Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					"""
					using Microsoft.UI.Xaml.Controls;

					namespace TestRepro
					{
						public sealed partial class MainPage : Page
						{
							public MainPage() { this.InitializeComponent(); }
						}
					}
					"""
				},
			},
			ReferenceAssemblies = _Dotnet.Current.WithUnoPackage(),
			// Opt-in is NOT set; feature must be fully inert.
		}.AddGeneratedSources();

		await test.RunAsync();
	}
}
