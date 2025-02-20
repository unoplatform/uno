using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_MarkupExtension
{
	[TestMethod]
	public async Task TestNullMarkupExtensionInRegularProperty()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
	<Page
		x:Class="TestRepro.MainPage"
		xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
		xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
		xmlns:local="using:TestRepro"
		xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
		xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
		mc:Ignorable="d">

		<Grid>
			<TextBox Text="{x:Null}" />
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
					using Windows.UI.Xaml;
					using Windows.UI.Xaml.Controls;

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
			}
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task TestNullMarkupExtensionInAttachedProperty()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
	<Page
		x:Class="TestRepro.MainPage"
		xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
		xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
		xmlns:local="using:TestRepro"
		xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
		xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
		mc:Ignorable="d">

		<Grid>
			<TextBox ToolTipService.ToolTip="{x:Null}" />
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
					using Windows.UI.Xaml;
					using Windows.UI.Xaml.Controls;

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
			}
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task TestMarkupExtensionWithShadowingFrameworkElementLoading()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
			<local:BasePage
				x:Class="TestRepro.MainPage"
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				xmlns:local="using:TestRepro"
				xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
				xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
				Background="{ThemeResource ApplicationPageBackgroundThemeBrush}"
				mc:Ignorable="d">
			</local:BasePage>
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
						public partial class BasePage : Page
						{
							public string Loading => "";
						}

						public sealed partial class MainPage : BasePage
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

	[TestMethod]
	public async Task TestMarkupExtensionWithShadowingWindowActivated()
	{
		var xamlFile = new XamlFile("MainWindow.xaml", """
			<local:BaseWindow
				x:Class="TestRepro.MainWindow"
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				xmlns:local="using:TestRepro"
				xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
				xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
				mc:Ignorable="d">

				<Page Background="{ThemeResource ApplicationPageBackgroundThemeBrush}" />
			</local:BaseWindow>
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
						public partial class BaseWindow : Window
						{
							public string Activated => "";
						}

						public sealed partial class MainWindow : BaseWindow
						{
							public MainWindow()
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
