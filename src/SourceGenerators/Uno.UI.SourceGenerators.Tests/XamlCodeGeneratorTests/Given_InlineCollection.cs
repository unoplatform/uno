using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

using Verify = XamlSourceGeneratorVerifier;

/// <summary>
/// Implicit items for a settable collection property create the collection when the property is null, like WinUI.
/// </summary>
[TestClass]
public class Given_InlineCollection
{
	[TestMethod]
	public async Task When_Implicit_Items_On_Settable_Collection()
	{
		var pageFile = new XamlFile("MainPage.xaml", """
			<Page
				x:Class="TestRepro.MainPage"
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<Page.Transitions>
					<EntranceThemeTransition />
				</Page.Transitions>
				<StackPanel>
					<StackPanel.ChildrenTransitions>
						<RepositionThemeTransition />
						<EntranceThemeTransition />
					</StackPanel.ChildrenTransitions>
					<Grid>
						<Grid.ChildrenTransitions>
							<TransitionCollection>
								<RepositionThemeTransition />
							</TransitionCollection>
						</Grid.ChildrenTransitions>
					</Grid>
				</StackPanel>
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
