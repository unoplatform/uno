using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests;

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_RelativeUri
{
	/// <summary>
	/// A relative URI is resolved against the base URI for ImageSource-typed properties, and rewritten to
	/// the MRT local-resource form for every other Uri-typed property, as measured on native WinUI.
	/// </summary>
	[TestMethod]
	public async Task When_Relative_Uri_Is_Rewritten()
	{
		var xamlFiles = new[]
		{
			new XamlFile("MainPage.xaml",
				"""
				<Page x:Class="TestRepro.MainPage"
					  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					  xmlns:local="using:TestRepro">
					<StackPanel>
						<local:UriHolder Uri="Assets/asset.png" />
						<local:UriHolder Uri="/Assets/asset.png" />
						<HyperlinkButton NavigateUri="Assets/doc.pdf" />
						<BitmapIcon UriSource="Assets/asset.png" />

						<Image Source="Assets/asset.png" />
						<Image Source="/Assets/asset.png" />
						<local:UriHolder ImageSource="Assets/asset.png" />
						<!-- A backslash has to survive the emitted literal rather than read as an escape. -->
						<local:UriHolder ImageSource="Assets\asset.png" />
						<Border>
							<Border.Background>
								<ImageBrush ImageSource="Assets/asset.png" />
							</Border.Background>
						</Border>
						<Image>
							<Image.Source>
								<BitmapImage UriSource="Assets/asset.png" />
							</Image.Source>
						</Image>
						<Image>
							<Image.Source>
								<SvgImageSource UriSource="Assets/asset.svg" />
							</Image.Source>
						</Image>
					</StackPanel>
				</Page>
				"""),
		};

		var test = new Verify.Test(xamlFiles)
		{
			TestState =
			{
				Sources =
				{
					"""
					using System;
					using Microsoft.UI.Xaml;
					using Microsoft.UI.Xaml.Controls;
					using Microsoft.UI.Xaml.Media;

					namespace TestRepro
					{
						public partial class UriHolder : Control
						{
							public Uri Uri
							{
								get => (Uri)GetValue(UriProperty);
								set => SetValue(UriProperty, value);
							}

							public static DependencyProperty UriProperty { get; } =
								DependencyProperty.Register(nameof(Uri), typeof(Uri), typeof(UriHolder), new FrameworkPropertyMetadata(default(Uri)));

							public ImageSource ImageSource
							{
								get => (ImageSource)GetValue(ImageSourceProperty);
								set => SetValue(ImageSourceProperty, value);
							}

							public static DependencyProperty ImageSourceProperty { get; } =
								DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(UriHolder), new FrameworkPropertyMetadata(default(ImageSource)));
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
			}
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	/// <summary>
	/// A Setter reaches the rewrite through the style's target type rather than the syntactic owner,
	/// so each property has to land on the same form it would inline.
	/// </summary>
	[TestMethod]
	public async Task When_Relative_Uri_In_Setter()
	{
		var xamlFiles = new[]
		{
			new XamlFile("MainPage.xaml",
				"""
				<Page x:Class="TestRepro.MainPage"
					  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					  xmlns:local="using:TestRepro">
					<Page.Resources>
						<Style x:Key="ImageStyle" TargetType="Image">
							<Setter Property="Source" Value="Assets/asset.png" />
						</Style>
						<Style x:Key="LinkStyle" TargetType="HyperlinkButton">
							<Setter Property="NavigateUri" Value="Assets/doc.pdf" />
						</Style>
						<Style x:Key="HolderStyle" TargetType="local:UriHolder">
							<Setter Property="Uri" Value="Assets/asset.png" />
							<Setter Property="ImageSource" Value="Assets/asset.png" />
						</Style>
					</Page.Resources>
					<StackPanel>
						<Image Style="{StaticResource ImageStyle}" />
						<HyperlinkButton Style="{StaticResource LinkStyle}" />
						<local:UriHolder Style="{StaticResource HolderStyle}" />
					</StackPanel>
				</Page>
				"""),
		};

		var test = new Verify.Test(xamlFiles)
		{
			TestState =
			{
				Sources =
				{
					"""
					using System;
					using Microsoft.UI.Xaml;
					using Microsoft.UI.Xaml.Controls;
					using Microsoft.UI.Xaml.Media;

					namespace TestRepro
					{
						public partial class UriHolder : Control
						{
							public Uri Uri
							{
								get => (Uri)GetValue(UriProperty);
								set => SetValue(UriProperty, value);
							}

							public static DependencyProperty UriProperty { get; } =
								DependencyProperty.Register(nameof(Uri), typeof(Uri), typeof(UriHolder), new FrameworkPropertyMetadata(default(Uri)));

							public ImageSource ImageSource
							{
								get => (ImageSource)GetValue(ImageSourceProperty);
								set => SetValue(ImageSourceProperty, value);
							}

							public static DependencyProperty ImageSourceProperty { get; } =
								DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(UriHolder), new FrameworkPropertyMetadata(default(ImageSource)));
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
			}
		}.AddGeneratedSources();

		await test.RunAsync();
	}
}
