using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;
using Verify = Uno.UI.SourceGenerators.Tests.Verifiers.XamlSourceGeneratorVerifier;

namespace Uno.UI.SourceGenerators.Tests.Windows_UI_Xaml_Controls.ParserTests;

public partial class Given_Parser
{
	[TestMethod]
	public async Task When_ClrNamespace_Xmlns_Unused()
	{
		var xamlFiles = new[]
		{
			new XamlFile("MainPage.xaml",
				"""
				<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:local="clr-namespace:TestRepro.Controls;assembly=TestRepro">

					<TextBlock Text="Hello" />
				</Page>

				"""),
		};

		var test = new Verify.Test(xamlFiles) { TestState = { Sources = { _emptyCodeBehind } } }.AddGeneratedSources();

		test.ExpectedDiagnostics.Add(
			DiagnosticResult.CompilerError("UXAML0006").WithSpan("//Project/0/MainPage.xaml", 4, 2, 4, 2).WithArguments(
				"""The 'clr-namespace:' XAML namespace form is not supported. Replace 'xmlns:local="clr-namespace:TestRepro.Controls;assembly=TestRepro"' with 'xmlns:local="using:TestRepro.Controls"'"""));

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_ClrNamespace_Xmlns_Used()
	{
		var xamlFiles = new[]
		{
			new XamlFile("MainPage.xaml",
				"""
				<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:local="clr-namespace:TestRepro">

					<local:MyControl />
				</Page>

				"""),
		};

		var test = new Verify.Test(xamlFiles) { TestState = { Sources = { _emptyCodeBehind, _myControl } } }.AddGeneratedSources();

		test.ExpectedDiagnostics.Add(
			DiagnosticResult.CompilerError("UXAML0006").WithSpan("//Project/0/MainPage.xaml", 4, 2, 4, 2).WithArguments(
				"""The 'clr-namespace:' XAML namespace form is not supported. Replace 'xmlns:local="clr-namespace:TestRepro"' with 'xmlns:local="using:TestRepro"'"""));

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_ClrNamespace_Xmlns_InlineScope()
	{
		var xamlFiles = new[]
		{
			new XamlFile("MainPage.xaml",
				"""
				<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

					<Page.Resources>
						<DataTemplate x:Key="Template" xmlns:local="clr-namespace:TestRepro.Controls">
							<TextBlock Text="Hello" />
						</DataTemplate>
					</Page.Resources>
				</Page>

				"""),
		};

		var test = new Verify.Test(xamlFiles) { TestState = { Sources = { _emptyCodeBehind } } }.AddGeneratedSources();

		test.ExpectedDiagnostics.Add(
			DiagnosticResult.CompilerError("UXAML0006").WithSpan("//Project/0/MainPage.xaml", 6, 34, 6, 34).WithArguments(
				"""The 'clr-namespace:' XAML namespace form is not supported. Replace 'xmlns:local="clr-namespace:TestRepro.Controls"' with 'xmlns:local="using:TestRepro.Controls"'"""));

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_ClrNamespace_Xmlns_Ignorable()
	{
		var xamlFiles = new[]
		{
			new XamlFile("MainPage.xaml",
				"""
				<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
					xmlns:d="clr-namespace:TestRepro.DesignTime"
					mc:Ignorable="d">

					<TextBlock Text="Hello" />
				</Page>

				"""),
		};

		var test = new Verify.Test(xamlFiles) { TestState = { Sources = { _emptyCodeBehind } } }.AddGeneratedSources();

		await test.RunAsync();
	}

	private const string _myControl = """
		namespace TestRepro
		{
			public partial class MyControl : Microsoft.UI.Xaml.Controls.UserControl
			{
			}
		}
		""";
}
