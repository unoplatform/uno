using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.Windows_UI_Xaml_Data.BindingTests;

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_Binding
{
	[TestMethod]
	public async Task When_Xaml_Object_With_Common_Properties()
	{
		var test = new TestSetup(xamlFileName: "Binding_Xaml_Object_With_Common_Properties.xaml", subFolder: Path.Combine("Uno.UI.Tests", "Windows_UI_Xaml_Data", "BindingTests", "Controls"));
		await Verify.AssertXamlGeneratorDiagnostics(test);
	}

	[TestMethod]
	public async Task When_Xaml_Object_With_Xaml_Object_Properties()
	{
		var test = new TestSetup(xamlFileName: "Binding_Xaml_Object_With_Xaml_Object_Properties.xaml", subFolder: Path.Combine("Uno.UI.Tests", "Windows_UI_Xaml_Data", "BindingTests", "Controls"));
		await Verify.AssertXamlGeneratorDiagnostics(test);
	}

	[TestMethod]
	public async Task When_Binding_ElementName_In_Template()
	{
		var test = new TestSetup(xamlFileName: "Binding_ElementName_In_Template.xaml", subFolder: Path.Combine("Uno.UI.Tests", "Windows_UI_Xaml_Data", "BindingTests", "Controls"))
		{
			PreprocessorSymbols =
			{
				"UNO_REFERENCE_API",
			},
		};
		await Verify.AssertXamlGeneratorDiagnostics(test);
	}

	[TestMethod]
	public async Task TestBaseTypeNotSpecifiedInCodeBehind()
	{
		var xamlFiles = new[]
		{
			new XamlFile("UserControl1.xaml", """
	<UserControl
		x:Class="TestRepro.UserControl1"
		xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
		xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
		xmlns:local="using:TestRepro"
		xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
		xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
		mc:Ignorable="d"
		d:DesignHeight="300"
		d:DesignWidth="400">

		<Grid></Grid>
	</UserControl>
	"""),
			new XamlFile("MainPage.xaml", """
	<Page
		x:Class="TestRepro.MainPage"
		xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
		xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
		xmlns:local="using:TestRepro"
		xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
		xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
		mc:Ignorable="d"
		Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

		<Grid>
			<TextBlock Text="Hello, world!" Margin="20" FontSize="30" />
			<local:UserControl1 DataContext="{Binding PreviewDropViewModel}"/>
		</Grid>
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
					namespace TestRepro
					{
						public sealed partial class UserControl1
						{
							public UserControl1()
							{
								this.InitializeComponent();
							}
						}
					}
					""",
					"""
					using Microsoft.UI.Xaml.Controls;

					namespace TestRepro
					{
						public sealed partial class MainPage : Page
						{
							public string PreviewDropViewModel { get; set; }

							public MainPage()
							{
								this.InitializeComponent();
							}
						}
					}
					"""
				}
			}
		};
		test.ExpectedDiagnostics.Add(
			// /0/Test0.cs(3,30): warning UXAML0002: TestRepro.UserControl1 does not explicitly define the Microsoft.UI.Xaml.Controls.UserControl base type in code behind.
			DiagnosticResult.CompilerWarning("UXAML0002").WithSpan(3, 30, 3, 42).WithArguments("TestRepro.UserControl1 does not explicitly define the Microsoft.UI.Xaml.Controls.UserControl base type in code behind.")
		);
		await test.RunAsync();
	}
}
