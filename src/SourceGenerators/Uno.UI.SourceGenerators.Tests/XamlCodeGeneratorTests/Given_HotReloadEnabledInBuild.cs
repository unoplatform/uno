using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_HotReloadEnabledInBuild
{
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
			GlobalConfigOverride = configOverride,
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task ResourceDictionaryCodeBehind()
	{
		var xamlFile = new XamlFile("MyDictionary.xaml", """
			<ResourceDictionary
			      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			      xmlns:local="using:TestRepro"
			      x:Class="Test.RD">
			</ResourceDictionary>
			""");

		var configOverride = new Dictionary<string, string> { { "build_property.UnoForceHotReloadCodeGen", "true" } };

		var test = new Verify.Test(xamlFile)
		{
			ReferenceAssemblies = _Dotnet.Current.WithUnoPackage(),
			GlobalConfigOverride = configOverride,
			TestState = { Sources = { "" } }
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
					using Microsoft.UI.Xaml;
					using Microsoft.UI.Xaml.Controls;
					namespace TestRepro;
					public sealed partial class MainPage : Page
					{
						public MainPage() => InitializeComponent();
					}
					"""
				}
			},
			ReferenceAssemblies = _Dotnet.Current.WithUnoPackage(),
			GlobalConfigOverride = configOverride,
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task SetOriginalSourceLocationIncludedInOutputForEmptyDataTemplates()
	{
		var xamlFile = new XamlFile("EmptyDataTemplatePage.xaml",
			"""
			 <Page x:Class="TestRepro.EmptyDataTemplatePage"
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:local="using:TestRepro"
					Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
				<Page.Resources>
					<DataTemplate x:Key="MyEmptyTemplate">
						<!-- SUT -->
					</DataTemplate>
				</Page.Resources>
				<StackPanel>
					<ListView ItemTemplate="{StaticResource MyItemTemplate}" />
					<Button x:Name="ButtonWithEmptyDataTemplate">
						<Button.ContentTemplate>
							<DataTemplate>
								<!-- SUT -->
							</DataTemplate>
						</Button.ContentTemplate>
					</Button>
				</StackPanel>
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
					using Microsoft.UI.Xaml;
					using Microsoft.UI.Xaml.Controls;
					namespace TestRepro
					{
						public sealed partial class EmptyDataTemplatePage : Page
						{
							public EmptyDataTemplatePage()
							{
								this.InitializeComponent();
							}
						}
					}
					"""
				}
			},
			ReferenceAssemblies = _Dotnet.Current.WithUnoPackage(),
			GlobalConfigOverride = configOverride,
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task SetOriginalSourceLocationInOutputForResourceDictionaryFile()
	{
		var dictionary = new XamlFile("MyDictionary.xaml", """
			<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
			  <ResourceDictionary.MergedDictionaries>
			    <ResourceDictionary>
			      <x:Double x:Key="MergedNumber">1</x:Double>
			    </ResourceDictionary>
			    <ResourceDictionary Source="ms-appx:///TestProject/0/OtherDictionary.xaml" />
			  </ResourceDictionary.MergedDictionaries>
			  <ResourceDictionary.ThemeDictionaries>
			    <ResourceDictionary x:Key="Light">
			      <x:Double x:Key="ThemedNumber">2</x:Double>
			    </ResourceDictionary>
			  </ResourceDictionary.ThemeDictionaries>
			  <x:Double x:Key="ImportantNumber">12</x:Double>
			</ResourceDictionary>
			""");

		var otherDictionary = new XamlFile("OtherDictionary.xaml", """
			<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
			  <x:Double x:Key="OtherNumber">3</x:Double>
			</ResourceDictionary>
			""");

		var configOverride = new Dictionary<string, string> { { "build_property.UnoForceHotReloadCodeGen", "true" } };

		var test = new Verify.Test([dictionary, otherDictionary])
		{
			ReferenceAssemblies = _Dotnet.Current.WithUnoPackage(),
			GlobalConfigOverride = configOverride,
			TestState = { Sources = { "" } }
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task SetOriginalSourceLocationInOutputForExplicitResourceDictionaries()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
			<Page x:Class="TestRepro.MainPage"
			      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			      xmlns:local="using:TestRepro">
			  <Page.Resources>
			    <ResourceDictionary>
			      <ResourceDictionary.ThemeDictionaries>
			        <ResourceDictionary x:Key="Default">
			          <SolidColorBrush x:Key="MyBrush" Color="Red" />
			        </ResourceDictionary>
			      </ResourceDictionary.ThemeDictionaries>
			      <x:Double x:Key="PageNumber">1</x:Double>
			    </ResourceDictionary>
			  </Page.Resources>
			  <Grid>
			    <Grid.Resources>
			      <ResourceDictionary>
			        <x:Double x:Key="GridNumber">2</x:Double>
			      </ResourceDictionary>
			    </Grid.Resources>
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
			GlobalConfigOverride = configOverride,
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task SetOriginalSourceLocationInOutputForTypedResourceDictionaries()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
			<Page x:Class="TestRepro.MainPage"
			      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			      xmlns:local="using:TestRepro">
			  <Page.Resources>
			    <ResourceDictionary>
			      <ResourceDictionary.MergedDictionaries>
			        <local:MyCodeDictionary />
			      </ResourceDictionary.MergedDictionaries>
			    </ResourceDictionary>
			  </Page.Resources>
			  <Grid>
			    <Grid.Resources>
			      <local:MyCodeDictionary />
			    </Grid.Resources>
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
					using Microsoft.UI.Xaml;
					using Microsoft.UI.Xaml.Controls;

					namespace TestRepro
					{
						// A dictionary defined in code, as the themes of a library are: it has no
						// InitializeComponent to stamp its own location.
						public class MyCodeDictionary : ResourceDictionary
						{
						}

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
			GlobalConfigOverride = configOverride,
		}.AddGeneratedSources();

		await test.RunAsync();
	}
}
