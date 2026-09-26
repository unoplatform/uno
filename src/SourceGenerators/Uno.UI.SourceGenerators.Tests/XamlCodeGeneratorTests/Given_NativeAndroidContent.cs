using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_NativeAndroidContent
{
	// The verifier loads the Skia build of Uno.WinRT, which has no ContextHelper.
	private const string ContextHelperStub =
		"""
		namespace Uno.UI
		{
			public static class ContextHelper
			{
				public static global::Android.Content.Context Current => null!;
			}
		}
		""";

	[TestMethod]
	[DataRow("<ContentControl><widget:TextView /></ContentControl>")]
	[DataRow("<ContentPresenter><widget:TextView /></ContentPresenter>")]
	[DataRow("<Grid><Grid.Resources><widget:TextView x:Key=\"NativeText\" /></Grid.Resources></Grid>")]
	public async Task When_Native_View_As_Object_Content(string content)
	{
		var xamlFile = new XamlFile(
			"MainPage.xaml",
			$$"""
			<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:widget="using:Android.Widget">
				{{content}}
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
					ContextHelperStub,
				}
			},
			ReferenceAssemblies = _Dotnet.CurrentAndroid.ReferenceAssemblies,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
		};

		await test.RunAsync();
	}
}
