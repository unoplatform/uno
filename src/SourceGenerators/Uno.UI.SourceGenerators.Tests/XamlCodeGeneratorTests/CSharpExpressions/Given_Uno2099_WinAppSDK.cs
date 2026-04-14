using System.Collections.Generic;
using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests.CSharpExpressions;

using Verify = XamlSourceGeneratorVerifier;

/// <summary>
/// Spec FR-014 / FR-015 / SC-004: XAML C# expressions are Uno-only; WinAppSDK builds
/// surface <c>UNO2099</c> and MUST NOT lower. The diagnostic points at the first
/// offending attribute and names the XAML file + attribute.
/// </summary>
/// <remarks>
/// Phase 2 note: the main <c>Uno.UI.SourceGenerators</c> is disabled on WinAppSDK via
/// <c>ShouldRunGenerator</c> in <c>Uno.UI.SourceGenerators.props</c>, so this diagnostic
/// path is defensive in depth. In this test we force the generator to run and simulate
/// <c>$(IsWinAppSdk)=true</c> via the global-config override to exercise UNO2099 emission.
/// This test is disabled pending the Phase 3 classifier hook that runs before
/// markup-extension parsing.
/// </remarks>
[TestClass]
public class Given_Uno2099_WinAppSDK
{
	[TestMethod]
	public async Task When_FeatureEnabled_OnWinAppSDK_FiresUno2099()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
			<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<TextBlock Text="{= Title}" />
			</Page>
			""");

		var configOverride = new Dictionary<string, string>
		{
			{ "build_property.UnoXamlCSharpExpressionsEnabled", "true" },
			{ "build_property.IsWinAppSdk", "true" },
		};

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
			GlobalConfigOverride = configOverride,
		}.AddGeneratedSources();

		test.ExpectedDiagnostics.Add(
			DiagnosticResult.CompilerError("UNO2099").WithArguments("C:/Project/0/MainPage.xaml", "Text"));

		await test.RunAsync();
	}
}
