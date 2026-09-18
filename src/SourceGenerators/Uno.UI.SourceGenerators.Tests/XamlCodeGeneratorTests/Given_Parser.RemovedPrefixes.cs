using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;
using Verify = Uno.UI.SourceGenerators.Tests.Verifiers.XamlSourceGeneratorVerifier;

namespace Uno.UI.SourceGenerators.Tests.Windows_UI_Xaml_Controls.ParserTests;

public partial class Given_Parser
{
	private const string PresentationNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

	[TestMethod]
	[DataRow("skia", "Use 'not_winappsdk' instead.")]
	[DataRow("netstdref", "Use 'not_winappsdk' instead.")]
	[DataRow("androidskia", "Use 'android' instead.")]
	[DataRow("iosskia", "Use 'ios' instead.")]
	[DataRow("tvosskia", "Use 'tvos' instead.")]
	[DataRow("wasmskia", "Use 'wasm' instead.")]
	[DataRow("macos", "Use 'desktop' instead.")]
	[DataRow("xamarin", "Drop the prefix from the markup using it.")]
	[DataRow("legacy", "Drop the prefix from the markup using it.")]
	public async Task When_Removed_Prefix_Is_Ignorable(string prefix, string advice)
	{
		var xamlFile = new XamlFile("MainPage.xaml",
			$$"""
			<Page x:Class="TestRepro.MainPage"
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
				xmlns:{{prefix}}="http://uno.ui/{{prefix}}"
				mc:Ignorable="{{prefix}}">

				<{{prefix}}:TextBlock Text="Hello" />
			</Page>

			""");

		var test = CreateDiagnosticsOnlyTest(xamlFile);

		test.ExpectedDiagnostics.Add(
			DiagnosticResult.CompilerWarning("UXAML0007").WithSpan("//Project/0/MainPage.xaml", 5, 2, 5, 2).WithArguments(
				$"The '{prefix}' conditional XAML prefix was removed in Uno Platform 7.0 and its content is now ignored on every target. {advice}"));

		await test.RunAsync();
	}

	[TestMethod]
	[DataRow("not_skia", "Use 'winappsdk' instead.")]
	[DataRow("not_netstdref", "Use 'winappsdk' instead.")]
	[DataRow("not_androidskia", "Use 'not_android' instead.")]
	[DataRow("not_iosskia", "Use 'not_ios' instead.")]
	[DataRow("not_tvosskia", "Use 'not_tvos' instead.")]
	[DataRow("not_wasmskia", "Use 'not_wasm' instead.")]
	[DataRow("not_macos", "Use 'not_desktop' instead.")]
	[DataRow("not_mux", "Remove it along with the markup using it; it dates from UWP support and never applied.")]
	public async Task When_Removed_Prefix_Aliases_Presentation_Namespace(string prefix, string advice)
	{
		var xamlFile = new XamlFile("MainPage.xaml",
			$$"""
			<Page x:Class="TestRepro.MainPage"
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				xmlns:{{prefix}}="{{PresentationNamespace}}">

				<TextBlock {{prefix}}:Text="Hello" />
			</Page>

			""");

		var test = CreateDiagnosticsOnlyTest(xamlFile);

		test.ExpectedDiagnostics.Add(
			DiagnosticResult.CompilerWarning("UXAML0007").WithSpan("//Project/0/MainPage.xaml", 4, 2, 4, 2).WithArguments(
				$"The '{prefix}' conditional XAML prefix was removed in Uno Platform 7.0 and its content now applies on every target. {advice}"));

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Removed_Prefix_Declared_In_Nested_Element()
	{
		var xamlFile = new XamlFile("MainPage.xaml",
			$$"""
			<Page x:Class="TestRepro.MainPage"
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

				<StackPanel xmlns:not_skia="{{PresentationNamespace}}">
					<TextBlock not_skia:Text="Hello" />
				</StackPanel>
			</Page>

			""");

		var test = CreateDiagnosticsOnlyTest(xamlFile);

		test.ExpectedDiagnostics.Add(
			DiagnosticResult.CompilerWarning("UXAML0007").WithSpan("//Project/0/MainPage.xaml", 5, 14, 5, 14).WithArguments(
				"The 'not_skia' conditional XAML prefix was removed in Uno Platform 7.0 and its content now applies on every target. Use 'winappsdk' instead."));

		await test.RunAsync();
	}

	[TestMethod]
	[DataRow("skia")]
	[DataRow("not_skia")]
	[DataRow("macos")]
	[DataRow("xamarin")]
	[DataRow("legacy")]
	public async Task When_Removed_Prefix_Is_Clr_Namespace_Alias(string prefix)
	{
		var xamlFile = new XamlFile("MainPage.xaml",
			$$"""
			<Page x:Class="TestRepro.MainPage"
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				xmlns:{{prefix}}="using:TestRepro">

				<{{prefix}}:MyControl />
			</Page>

			""");

		var test = CreateDiagnosticsOnlyTest(xamlFile, _myControl);

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Current_Conditional_Prefixes_Are_Used()
	{
		var xamlFile = new XamlFile("MainPage.xaml",
			$$"""
			<Page x:Class="TestRepro.MainPage"
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
				xmlns:android="http://uno.ui/android"
				xmlns:not_winappsdk="http://uno.ui/not_winappsdk"
				xmlns:winappsdk="{{PresentationNamespace}}"
				xmlns:not_desktop="{{PresentationNamespace}}"
				mc:Ignorable="android not_winappsdk">

				<StackPanel>
					<android:TextBlock Text="Android" />
					<not_winappsdk:TextBlock Text="Uno" />
					<TextBlock winappsdk:Text="WinAppSDK" not_desktop:Tag="NotDesktop" />
				</StackPanel>
			</Page>

			""");

		var test = CreateDiagnosticsOnlyTest(xamlFile);

		await test.RunAsync();
	}

	private static Verify.Test CreateDiagnosticsOnlyTest(XamlFile xamlFile, string? additionalSource = null)
	{
		var test = new Verify.Test(xamlFile)
		{
			TestState = { Sources = { _emptyCodeBehind } },
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
		};

		if (additionalSource is not null)
		{
			test.TestState.Sources.Add(additionalSource);
		}

		return test;
	}
}
