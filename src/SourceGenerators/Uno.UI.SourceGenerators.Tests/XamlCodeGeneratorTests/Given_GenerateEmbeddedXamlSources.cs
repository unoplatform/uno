using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_GenerateEmbeddedXamlSources
{
	[TestMethod]
	public async Task EscapeTripleQuotes()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """"""
		                                             <Page x:Class="TestRepro.MainPage"
		                                                 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
		                                                 xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
		                                             <!-- This is a comment having potential escaping problems: """ -->
		                                             <!-- This is a comment having potential escaping problems: """ -->
		                                             </Page>
		                                             """""");

		var configOverride = new Dictionary<string, string> { { "build_property.UnoGenerateXamlSourcesProvider", "true" } };

		var test = new Verify.Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					"""
					using Microsoft.UI.Xaml;
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
					"""
				}
			},
			ReferenceAssemblies = _Dotnet.Current.WithUnoPackage(),
			DisableBuildReferences = true,
			GlobalConfigOverride = configOverride,
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task EscapeNQuotes()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """"""
		                                             <Page x:Class="TestRepro.MainPage"
		                                                 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
		                                                 xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
		                                             <!-- This is a comment having potential escaping problems: " "" """ """" """"" -->
		                                             <!-- This is a comment having potential escaping problems: " "" """ """" """"" -->
		                                             </Page>
		                                             """""");

		var configOverride = new Dictionary<string, string> { { "build_property.UnoGenerateXamlSourcesProvider", "true" } };

		var test = new Verify.Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					"""
					using Microsoft.UI.Xaml;
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
					"""
				}
			},
			ReferenceAssemblies = _Dotnet.Current.WithUnoPackage(),
			DisableBuildReferences = true,
			GlobalConfigOverride = configOverride,
		}.AddGeneratedSources();

		await test.RunAsync();
	}
}
