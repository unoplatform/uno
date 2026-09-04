using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_NoFuzzyMatching
{
	[TestMethod]
	public async Task TestVisibleBoundsPadding()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
			<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
					xmlns:uub="using:Uno.UI.Behaviors"
					mc:Ignorable="android ios">

				<Grid uub:VisibleBoundsPadding.PaddingMask="Top">
					<TextBlock Text="Hello, world!"
							   Margin="20"
							   FontSize="30" />
				</Grid>

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
			ReferenceAssemblies = _Dotnet.CurrentAndroid.WithUnoPackage(),
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task TestTypePropertyWithXString()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
			<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
					xmlns:local="using:TestRepro"
					xmlns:uub="using:Uno.UI.Behaviors"
					mc:Ignorable="android ios">

				<local:MyButton MyProperty="x:String" />

			</Page>
			""");

		var test = new Verify.Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					"""
					using System;
					using Microsoft.UI.Xaml;
					using Microsoft.UI.Xaml.Controls;

					namespace TestRepro
					{
						public sealed partial class MyButton : Button
						{
							public Type MyProperty { get; set; }
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
		}.AddGeneratedSources();

		await test.RunAsync();
	}
}
