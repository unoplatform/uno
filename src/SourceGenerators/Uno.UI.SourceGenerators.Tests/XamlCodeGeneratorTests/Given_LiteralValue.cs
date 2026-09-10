using System.Threading.Tasks;
using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_LiteralValue
{
	[TestMethod]
	public async Task When_Object_Property_With_Quotes()
	{
		// A string literal containing double-quotes assigned to an object-typed
		// property must be emitted as a valid, escaped C# string literal.
		var xamlFile = new XamlFile("MainPage.xaml", """
			<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
					xmlns:local="using:TestRepro"
					mc:Ignorable="android ios">

				<local:MyControl Value="IsEnabled=&quot;False&quot; " />

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
						public sealed partial class MyControl : Control
						{
							public object Value { get; set; }
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
