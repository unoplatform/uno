using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

using Verify = XamlSourceGeneratorVerifier;

/// <summary>
/// Hot Reload code-gen hoists apply blocks into class-level ApplyTo_* methods, which cannot see a
/// resource owner that lives only as a lambda parameter. https://github.com/unoplatform/uno/issues/24292
/// </summary>
[TestClass]
public class Given_HoistedResourceOwner
{
	// A lazily-initialized resource introduces __ResourceOwner_N as a WeakResourceInitializer lambda
	// parameter; the markup extension's parser context needs it from inside the hoisted method.
	[TestMethod]
	public async Task When_CustomMarkupExtension_In_Lazy_Resource()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
	<Page
		x:Class="TestRepro.MainPage"
		xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
		xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
		xmlns:local="using:TestRepro"
		xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
		xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
		mc:Ignorable="d">

		<Page.Resources>
			<ResourceDictionary>
				<SymbolIconSource x:Key="DeleteSymbol" Symbol="Delete" />
				<SwipeItems x:Key="HistorySwipeItems" Mode="Execute">
					<SwipeItem AutomationProperties.Name="{local:ResourceString Name=DeleteHistorySwipeItem}"
							   IconSource="{StaticResource DeleteSymbol}" />
				</SwipeItems>
			</ResourceDictionary>
		</Page.Resources>

		<Grid />
	</Page>
	""");

		var test = new Verify.Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					"""
					using Microsoft.UI.Xaml;
					using Microsoft.UI.Xaml.Controls;
					using Microsoft.UI.Xaml.Markup;

					namespace TestRepro
					{
						public sealed partial class MainPage : Page
						{
							public MainPage()
							{
								this.InitializeComponent();
							}
						}

						public sealed class ResourceStringExtension : MarkupExtension
						{
							public string Name { get; set; }

							protected override object ProvideValue() => Name;
						}
					}
					"""
				}
			},
			ReferenceAssemblies = _Dotnet.Current.WithUnoPackage(),
			GlobalConfigOverride = new Dictionary<string, string> { { "build_property.UnoForceHotReloadCodeGen", "true" } },
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	// Nested inside a template the owner has to travel alongside __settings, which is the only shape
	// that needs the five-argument GenericApply overload.
	[TestMethod]
	public async Task When_CustomMarkupExtension_In_Lazy_Resource_Inside_Template()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
	<Page
		x:Class="TestRepro.MainPage"
		xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
		xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
		xmlns:local="using:TestRepro"
		xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
		xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
		mc:Ignorable="d">

		<Page.Resources>
			<DataTemplate x:Key="ItemTemplate">
				<Grid>
					<Grid.Resources>
						<SymbolIconSource x:Key="DeleteSymbol" Symbol="Delete" />
						<SwipeItems x:Key="ItemSwipeItems" Mode="Execute">
							<SwipeItem AutomationProperties.Name="{local:ResourceString Name=DeleteSwipeItem}"
									   IconSource="{StaticResource DeleteSymbol}" />
						</SwipeItems>
					</Grid.Resources>
					<TextBlock Text="Item" />
				</Grid>
			</DataTemplate>
		</Page.Resources>

		<Grid />
	</Page>
	""");

		var test = new Verify.Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					"""
					using Microsoft.UI.Xaml;
					using Microsoft.UI.Xaml.Controls;
					using Microsoft.UI.Xaml.Markup;

					namespace TestRepro
					{
						public sealed partial class MainPage : Page
						{
							public MainPage()
							{
								this.InitializeComponent();
							}
						}

						public sealed class ResourceStringExtension : MarkupExtension
						{
							public string Name { get; set; }

							protected override object ProvideValue() => Name;
						}
					}
					"""
				}
			},
			ReferenceAssemblies = _Dotnet.Current.WithUnoPackage(),
			GlobalConfigOverride = new Dictionary<string, string> { { "build_property.UnoForceHotReloadCodeGen", "true" } },
		}.AddGeneratedSources();

		await test.RunAsync();
	}
}
