using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;
using Verify = Uno.UI.SourceGenerators.Tests.Verifiers.XamlSourceGeneratorVerifier;

namespace Uno.UI.SourceGenerators.Tests.Windows_UI_Xaml_Controls.ParserTests;

public partial class Given_Parser
{
	private const string RemovedToolkitNamespaceAdvice =
		"Point it at the namespace of the types the markup uses: 'using:Uno.UI.Xaml.Controls' for ElevatedView, CommandBarExtensions and SplitViewExtensions, "
		+ "'using:Uno.UI.Behaviors' for VisibleBoundsPadding, 'using:Uno.UI.Xaml' for UIElementExtensions, 'using:Uno.UI.Xaml.Automation' for AutomationPropertiesExtensions, "
		+ "and 'using:Uno.UI.Xaml.Controls.Primitives' for ScrollBarExtensions. MenuFlyoutExtensions and MenuFlyoutItemExtensions were removed.";

	[TestMethod]
	public async Task When_Removed_Toolkit_Namespace_Is_Declared()
	{
		var xamlFile = new XamlFile("MainPage.xaml",
			"""
			<Page x:Class="TestRepro.MainPage"
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				xmlns:toolkit="using:Uno.UI.Toolkit">

				<TextBlock Text="Hello" />
			</Page>

			""");

		var test = CreateDiagnosticsOnlyTest(xamlFile);

		test.ExpectedDiagnostics.Add(
			DiagnosticResult.CompilerWarning("UXAML0009").WithSpan("//Project/0/MainPage.xaml", 4, 2, 4, 2).WithArguments(
				$"The 'Uno.UI.Toolkit' namespace was removed in Uno Platform 7.0, so 'xmlns:toolkit=\"using:Uno.UI.Toolkit\"' no longer resolves. {RemovedToolkitNamespaceAdvice}"));

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Removed_Toolkit_Namespace_Declared_In_Nested_Element()
	{
		var xamlFile = new XamlFile("MainPage.xaml",
			"""
			<Page x:Class="TestRepro.MainPage"
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

				<StackPanel xmlns:utu="using:Uno.UI.Toolkit">
					<TextBlock Text="Hello" />
				</StackPanel>
			</Page>

			""");

		var test = CreateDiagnosticsOnlyTest(xamlFile);

		test.ExpectedDiagnostics.Add(
			DiagnosticResult.CompilerWarning("UXAML0009").WithSpan("//Project/0/MainPage.xaml", 5, 14, 5, 14).WithArguments(
				$"The 'Uno.UI.Toolkit' namespace was removed in Uno Platform 7.0, so 'xmlns:utu=\"using:Uno.UI.Toolkit\"' no longer resolves. {RemovedToolkitNamespaceAdvice}"));

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Removed_Toolkit_Namespace_Is_Ignorable()
	{
		var xamlFile = new XamlFile("MainPage.xaml",
			"""
			<Page x:Class="TestRepro.MainPage"
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
				xmlns:toolkit="using:Uno.UI.Toolkit"
				mc:Ignorable="toolkit">

				<TextBlock Text="Hello" />
			</Page>

			""");

		await CreateDiagnosticsOnlyTest(xamlFile).RunAsync();
	}

	[TestMethod]
	[DataRow("using:Uno.UI.Toolkit.Samples")]
	[DataRow("using:Uno.Toolkit.UI")]
	public async Task When_Namespace_Only_Resembles_Removed_Toolkit_Namespace(string @namespace)
	{
		var xamlFile = new XamlFile("MainPage.xaml",
			$$"""
			<Page x:Class="TestRepro.MainPage"
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				xmlns:toolkit="{{@namespace}}">

				<TextBlock Text="Hello" />
			</Page>

			""");

		await CreateDiagnosticsOnlyTest(xamlFile).RunAsync();
	}
}
