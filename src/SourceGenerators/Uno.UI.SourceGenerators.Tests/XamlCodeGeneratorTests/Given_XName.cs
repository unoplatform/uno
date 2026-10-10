using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

using Verify = XamlSourceGeneratorVerifier;

/// <summary>
/// <c>x:Name</c> is WinUI's DependencyObject_Name: a DependencyObject without a settable <c>Name</c>
/// property (MenuFlyout, Storyboard, brushes...) stores it through <c>MarkupHelper.SetXName</c>.
/// </summary>
[TestClass]
public class Given_XName
{
	[TestMethod]
	public async Task When_XName_On_NonFrameworkElement_DependencyObject()
	{
		var pageFile = new XamlFile("MainPage.xaml", """
			<Page
				x:Class="TestRepro.MainPage"
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<Page.Resources>
					<SolidColorBrush x:Key="BrushKey" x:Name="NamedBrush" Color="Red" />
				</Page.Resources>
				<Grid>
					<VisualStateManager.VisualStateGroups>
						<VisualStateGroup x:Name="Group">
							<VisualState x:Name="State">
								<Storyboard x:Name="NamedStoryboard" />
							</VisualState>
						</VisualStateGroup>
					</VisualStateManager.VisualStateGroups>
					<Button x:Name="NamedButton">
						<Button.Flyout>
							<MenuFlyout x:Name="NamedMenuFlyout">
								<MenuFlyoutItem Text="Item" />
							</MenuFlyout>
						</Button.Flyout>
					</Button>
				</Grid>
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
