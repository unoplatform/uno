using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests;

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_ResourceDictionary
{
	[TestMethod]
	public async Task TestTwoLevelNestedMergedDictionariesWithSingleResourceDictionary()
	{
		var xamlFile = new XamlFile("MyResourceDictionary.xaml", """
			<ResourceDictionary
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">
				<ResourceDictionary.MergedDictionaries>
						<ResourceDictionary Source="ms-appx:///Path/To/File1.xaml">
							<ResourceDictionary.MergedDictionaries>
								<ResourceDictionary Source="ms-appx:///Path/To/File2.xaml" />
							</ResourceDictionary.MergedDictionaries>
						</ResourceDictionary>
				</ResourceDictionary.MergedDictionaries>
			</ResourceDictionary>
			""");

		var test = new Verify.Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					string.Empty, // https://github.com/dotnet/roslyn-sdk/issues/1121
				}
			}
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task TestTwoLevelNestedMergedDictionariesWithTwoResourceDictionaries()
	{
		var xamlFile = new XamlFile("MyResourceDictionary.xaml", """
			<ResourceDictionary
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">
				<ResourceDictionary.MergedDictionaries>
						<ResourceDictionary Source="ms-appx:///Path/To/File1.xaml">
							<ResourceDictionary.MergedDictionaries>
								<ResourceDictionary Source="ms-appx:///Path/To/File2.xaml" />
								<ResourceDictionary Source="ms-appx:///Path/To/File3.xaml" />
							</ResourceDictionary.MergedDictionaries>
						</ResourceDictionary>
				</ResourceDictionary.MergedDictionaries>
			</ResourceDictionary>
			""");

		var test = new Verify.Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					string.Empty, // https://github.com/dotnet/roslyn-sdk/issues/1121
				}
			}
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task TestThreeLevelNestedMergedDictionaries()
	{
		var xamlFile = new XamlFile("MyResourceDictionary.xaml", """
			<ResourceDictionary
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">
				<ResourceDictionary.MergedDictionaries>
						<ResourceDictionary Source="ms-appx:///Path/To/File1.xaml">
							<ResourceDictionary.MergedDictionaries>
								<ResourceDictionary Source="ms-appx:///Path/To/File2.xaml">
									<ResourceDictionary.MergedDictionaries>
										<ResourceDictionary Source="ms-appx:///Path/To/File3.xaml" />
									</ResourceDictionary.MergedDictionaries>
								</ResourceDictionary>
							</ResourceDictionary.MergedDictionaries>
						</ResourceDictionary>
				</ResourceDictionary.MergedDictionaries>
			</ResourceDictionary>
			""");

		var test = new Verify.Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					string.Empty, // https://github.com/dotnet/roslyn-sdk/issues/1121
				}
			}
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task TestResourceDictionaryAsAttachedPropertyShouldSetTheProperty()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
			<Page
				x:Class="TestRepro.MainPage"
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				xmlns:local="using:TestRepro"
				xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
				xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
				mc:Ignorable="d"
				Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

				<StackPanel>
					<Button Content="Button">
						<local:MyClass.X>
							<ResourceDictionary>
								<Color x:Key="PrimaryColor">Yellow</Color>
								<Color x:Key="SecondaryColor">Red</Color>
							</ResourceDictionary>
						</local:MyClass.X>
					</Button>

				</StackPanel>
			</Page>
			""");

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
						public class MyClass
						{
							public static readonly DependencyProperty XProperty = DependencyProperty.RegisterAttached("X", typeof(ResourceDictionary), typeof(MyClass), new PropertyMetadata(default));

							public static void SetX(DependencyObject element, ResourceDictionary value) => element.SetValue(XProperty, value);

							public static ResourceDictionary GetX(DependencyObject element) => (ResourceDictionary)element.GetValue(XProperty);
						}

						public partial class MainPage : Page
						{
							public MainPage()
							{
								this.InitializeComponent();
							}
						}
					}
					"""
				}
			}
		}.AddGeneratedSources();

		await test.RunAsync();
	}
}
