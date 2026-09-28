using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

using Verify = XamlSourceGeneratorVerifier;

/// <summary>
/// <c>x:Name</c> on a type whose <c>Name</c> is get-only (VisualState, VisualStateGroup, TextElement)
/// is applied through <c>MarkupHelper.SetXName</c>.
/// </summary>
[TestClass]
public class Given_GetOnlyName
{
	[TestMethod]
	public async Task When_XName_On_Resource_With_GetOnly_Name()
	{
		var pageFile = new XamlFile("MainPage.xaml", """
			<Page
				x:Class="TestRepro.MainPage"
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<Page.Resources>
					<VisualState x:Key="StateKey" x:Name="StateName" />
					<Run x:Key="RunKey" x:Name="RunName" Text="Hello" />
				</Page.Resources>
				<Grid />
			</Page>
			""");

		// A direct child of a ResourceDictionary skips the element-name block, so x:Name needs its own path there.
		var dictionaryFile = new XamlFile("Dictionary.xaml", """
			<ResourceDictionary
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<VisualState x:Key="DictionaryStateKey" x:Name="DictionaryStateName" />
				<Run x:Key="DictionaryRunKey" x:Name="DictionaryRunName" Text="Hello" />
			</ResourceDictionary>
			""");

		var test = new Verify.Test([pageFile, dictionaryFile])
		{
			TestState =
			{
				Sources =
				{
					"""
					using Microsoft.UI.Xaml.Controls;

					namespace TestRepro;

					public sealed partial class MainPage : Page
					{
						public MainPage()
						{
							this.InitializeComponent();
						}
					}
					"""
				}
			},
			ReferenceAssemblies = _Dotnet.Current.WithUnoPackage(),
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	// Intentional WinUI divergence: WinUI's XAML compiler rejects a plain Name on a TextElement (WMC0050, Name is get-only),
	// but its docs describe TextElement.Name as settable from XAML and its runtime parser accepts it, so Uno does too.
	[TestMethod]
	public async Task When_Plain_Name_On_TextElement()
	{
		var pageFile = new XamlFile("MainPage.xaml", """
			<Page
				x:Class="TestRepro.MainPage"
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<TextBlock>
					<Run Name="PlainRun" Text="a" />
					<Hyperlink Name="PlainLink">link</Hyperlink>
				</TextBlock>
			</Page>
			""");

		var test = new Verify.Test(pageFile)
		{
			TestState =
			{
				Sources =
				{
					"""
					using Microsoft.UI.Xaml.Controls;

					namespace TestRepro;

					public sealed partial class MainPage : Page
					{
						public MainPage()
						{
							this.InitializeComponent();
						}
					}
					"""
				}
			},
			ReferenceAssemblies = _Dotnet.Current.WithUnoPackage(),
		}.AddGeneratedSources();

		await test.RunAsync();
	}
}
