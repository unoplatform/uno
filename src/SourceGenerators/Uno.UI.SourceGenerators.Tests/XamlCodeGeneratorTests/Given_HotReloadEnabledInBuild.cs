using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_HotReloadEnabledInBuild
{
	// Specify this centrally for all tests that rely on Uno5 or above
	private static readonly ReferenceAssemblies _net7Uno5Refs = ReferenceAssemblies.Net.Net80.AddPackages(ImmutableArray.Create(new PackageIdentity("Uno.WinUI", "5.0.118")));

	[TestMethod]
	public async Task SetBaseUriIncludedInOutputForFrameworkElements()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
			<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
					xmlns:toolkit="using:Uno.UI.Toolkit"
					mc:Ignorable="android ios">

				<Grid toolkit:VisibleBoundsPadding.PaddingMask="Top">
					<TextBlock Text="Hello, world!"
							   Margin="20"
							   FontSize="30" />
				</Grid>

			</Page>
			""");

		var configOverride = new Dictionary<string, string> { { "build_property.UnoForceHotReloadCodeGen", "true" } };

		var test = new Verify.Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					"""
					using Windows.UI.Xaml;
					using Windows.UI.Xaml.Controls;

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
			ReferenceAssemblies = _net7Uno5Refs,
			DisableBuildReferences = true,
			GlobalConfigOverride = configOverride,
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task SetOriginalSourceLocationInOutputForPageLevelStyles()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
			<Page x:Class="TestRepro.MainPage"
			      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			      xmlns:local="using:TestRepro"
			      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
			  <Page.Resources>
			    <Style TargetType="TextBlock">
			      <Setter Property="Foreground" Value="Red" />
			    </Style>
			    <Style TargetType="Button" x:Key="MyCustomButtonStyle">
			      <Setter Property="Background" Value="Azure" />
			    </Style>
			  </Page.Resources>
			  <ListView />
			</Page>
			""");

		var configOverride = new Dictionary<string, string> { { "build_property.UnoForceHotReloadCodeGen", "true" } };

		var test = new Verify.Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					"""
					using Windows.UI.Xaml;
					using Windows.UI.Xaml.Controls;

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
			ReferenceAssemblies = _net7Uno5Refs,
			DisableBuildReferences = true,
			GlobalConfigOverride = configOverride,
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task SetOriginalSourceLocationInOutputForResourceTypes()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
			<Page x:Class="TestRepro.MainPage"
			      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			      xmlns:local="using:TestRepro"
			      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
			  <Page.Resources>
				<x:Double x:Key="ImportantNumber">12</x:Double>
				<x:String x:Key="ImportantMessage">Do more testing</x:String>
				<TextBlock x:Key="MyTextBlockResource" Text="use me" />
			  </Page.Resources>
			  <TextBlock Text="Some content" />
			</Page>
			""");

		var configOverride = new Dictionary<string, string> { { "build_property.UnoForceHotReloadCodeGen", "true" } };

		var test = new Verify.Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					"""
					using Windows.UI.Xaml;
					using Windows.UI.Xaml.Controls;

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
			ReferenceAssemblies = _net7Uno5Refs,
			DisableBuildReferences = true,
			GlobalConfigOverride = configOverride,
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task SetOriginalSourceLocationInOutputForPageLevelResources()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
			<Page x:Class="TestRepro.MainPage"
			      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			      xmlns:local="using:TestRepro"
			      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
			  <Page.Resources>
			    <Style TargetType="TextBlock">
			      <Setter Property="Foreground" Value="Red" />
			    </Style>
			    <Style TargetType="Button" x:Key="MyCustomButtonStyle">
			      <Setter Property="Background" Value="Azure" />
			    </Style>
			    <DataTemplate x:Key="MyItemTemplate">
			      <StackPanel>
			        <TextBlock Text="{Binding }" />
			        <Button Content="DoSomething" Style="{StaticResource MyCustomButtonStyle}" />
			      </StackPanel>
			    </DataTemplate>
			  </Page.Resources>
			  <ListView />
			</Page>
			""");

		var configOverride = new Dictionary<string, string> { { "build_property.UnoForceHotReloadCodeGen", "true" } };

		var test = new Verify.Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					"""
					using Windows.UI.Xaml;
					using Windows.UI.Xaml.Controls;

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
			ReferenceAssemblies = _net7Uno5Refs,
			DisableBuildReferences = true,
			GlobalConfigOverride = configOverride,
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task SetOriginalSourceLocationInOutputForTopLevelResourceDictionaries()
	{
		var xamlFile = new XamlFile("MyDictionary.xaml", """
			<ResourceDictionary
			      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			      xmlns:local="using:TestRepro"
				  x:Class="TestNamespace.TestClass">
			</ResourceDictionary>
			""");

		var configOverride = new Dictionary<string, string> { { "build_property.UnoForceHotReloadCodeGen", "true" } };

		var test = new Verify.Test(xamlFile)
		{
			ReferenceAssemblies = _net7Uno5Refs,
			DisableBuildReferences = true,
			GlobalConfigOverride = configOverride,
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task SetOriginalSourceLocationIncludedInOutputForDependencyObjectsThatArentFrameworkElements()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
			<Page x:Class="TestRepro.MainPage"
			      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			      xmlns:local="using:TestRepro"
			      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
			  <Page.Resources>
			    <Style TargetType="TextBlock">
			      <Setter Property="Foreground" Value="Red" />
			    </Style>
			    <Style TargetType="Button" x:Key="MyCustomButtonStyle">
			      <Setter Property="Background" Value="Azure" />
			    </Style>
			    <DataTemplate x:Key="MyItemTemplate">
			      <StackPanel>
			        <TextBlock Text="{Binding }" />
			        <Button Content="DoSomething" Style="{StaticResource MyCustomButtonStyle}" />
			      </StackPanel>
			    </DataTemplate>
			  </Page.Resources>
			  <VisualStateManager.VisualStateGroups>
			    <VisualStateGroup>
			      <VisualState x:Name="WideState">
			        <VisualState.StateTriggers>
			          <AdaptiveTrigger MinWindowWidth="641" />
			        </VisualState.StateTriggers>
			        <VisualState.Setters>
			          <Setter Target="TheListView.Background" Value="Red" />
			        </VisualState.Setters>
			      </VisualState>
			      <VisualState x:Name="NarrowState">
			        <VisualState.StateTriggers>
			          <AdaptiveTrigger MinWindowWidth="0" />
			        </VisualState.StateTriggers>
			        <VisualState.Setters>
			          <Setter Target="TheListView.Background" Value="Green" />
			        </VisualState.Setters>
			      </VisualState>
			    </VisualStateGroup>
			  </VisualStateManager.VisualStateGroups>
			  <ListView x:Name="TheListView" ItemTemplate="{StaticResource MyItemTemplate}">
			    <ListView.HeaderTemplate>
			      <DataTemplate>
			        <TextBlock Text="Header" />
			      </DataTemplate>
			    </ListView.HeaderTemplate>
			  </ListView>
			</Page>
			""");

		var configOverride = new Dictionary<string, string> { { "build_property.UnoForceHotReloadCodeGen", "true" } };

		var test = new Verify.Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					"""
					using Windows.UI.Xaml;
					using Windows.UI.Xaml.Controls;

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
			ReferenceAssemblies = _net7Uno5Refs,
			DisableBuildReferences = true,
			GlobalConfigOverride = configOverride,
		}.AddGeneratedSources();

		await test.RunAsync();
	}
}
