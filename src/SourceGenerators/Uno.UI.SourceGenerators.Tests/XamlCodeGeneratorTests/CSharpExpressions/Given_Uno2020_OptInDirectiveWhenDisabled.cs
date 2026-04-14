using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests.CSharpExpressions;

using Verify = XamlSourceGeneratorVerifier;

/// <summary>
/// Spec FR-013a: when the feature is OFF and a XAML file contains an unambiguous opt-in directive
/// (<c>{= ...}</c>, <c>{.Member}</c>, <c>{this.Member}</c>), the generator emits <c>UNO2020</c>
/// pointing at the offending attribute rather than letting the confusing markup-extension
/// parse error propagate.
/// </summary>
/// <remarks>
/// Phase 2 note: the classifier hook at <c>XamlFileGenerator.TryHandleCSharpExpression</c>
/// currently runs against <c>XamlMemberDefinition.Value</c>. Uno's XAML parser may interpret
/// the <c>{= ...}</c> form as a markup extension before our classifier sees it, depending on
/// the attribute type. Wiring the classifier earlier in the pipeline (so all directive forms
/// reach <c>TryClassify</c>) lands in Phase 3 / T014 follow-up. This test is temporarily
/// disabled until the earlier hook is in place — documenting the intended contract.
/// </remarks>
[TestClass]
public class Given_Uno2020_OptInDirectiveWhenDisabled
{
	[TestMethod]
	public async Task When_FeatureDisabled_ExplicitDirective_FiresUno2020()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
			<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<TextBlock Text="{= Title}" />
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
							public string Title { get; set; } = string.Empty;
							public MainPage() { this.InitializeComponent(); }
						}
					}
					"""
				},
			},
			ReferenceAssemblies = _Dotnet.Current.WithUnoPackage(),
			// Opt-in NOT set — UNO2020 must fire.
		}.AddGeneratedSources();

		test.ExpectedDiagnostics.Add(
			DiagnosticResult.CompilerError("UNO2020").WithArguments("Text"));

		await test.RunAsync();
	}
}
