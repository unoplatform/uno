using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_Storyboard
{
	[TestMethod]
	public async Task When_TargetName_Missing_In_Template()
	{
		// Mirrors the MediaTransportControls Focused state in WinUI's generic.xaml, which targets
		// FocusVisualWhite/FocusVisualBlack although the template defines neither (#24432).
		var xamlFile = new XamlFile("Themes.xaml", """
			<ResourceDictionary
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<Style TargetType="ContentControl">
					<Setter Property="Template">
						<Setter.Value>
							<ControlTemplate TargetType="ContentControl">
								<Grid x:Name="RootGrid">
									<VisualStateManager.VisualStateGroups>
										<VisualStateGroup x:Name="FocusStates">
											<VisualState x:Name="Focused">
												<Storyboard>
													<DoubleAnimation Storyboard.TargetName="FocusVisualWhite"
														Storyboard.TargetProperty="Opacity"
														To="1"
														Duration="0" />
													<DoubleAnimation Storyboard.TargetName="FocusVisualBlack"
														Storyboard.TargetProperty="Opacity"
														To="1"
														Duration="0" />
													<DoubleAnimation Storyboard.TargetName="RootGrid"
														Storyboard.TargetProperty="Opacity"
														To="1"
														Duration="0" />
												</Storyboard>
											</VisualState>
											<VisualState x:Name="Unfocused" />
										</VisualStateGroup>
									</VisualStateManager.VisualStateGroups>
									<TextBlock Text="{Binding Opacity, ElementName=FocusVisualWhite}" />
								</Grid>
							</ControlTemplate>
						</Setter.Value>
					</Setter>
				</Style>
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
			},
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
		};

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_TargetName_Missing_In_Page()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
			<Page x:Class="TestRepro.MainPage"
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<Grid x:Name="RootGrid">
					<VisualStateManager.VisualStateGroups>
						<VisualStateGroup>
							<VisualState x:Name="Highlighted">
								<Storyboard>
									<DoubleAnimation Storyboard.TargetName="MissingElement"
										Storyboard.TargetProperty="Opacity"
										To="0.5"
										Duration="0" />
									<DoubleAnimation Storyboard.TargetName="RootGrid"
										Storyboard.TargetProperty="Opacity"
										To="0.5"
										Duration="0" />
								</Storyboard>
							</VisualState>
						</VisualStateGroup>
					</VisualStateManager.VisualStateGroups>
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
					""",
				}
			},
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
		};

		await test.RunAsync();
	}
}
