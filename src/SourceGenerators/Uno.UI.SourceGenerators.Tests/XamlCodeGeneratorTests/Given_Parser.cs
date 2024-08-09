using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.Windows_UI_Xaml_Controls.ParserTests;

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_Parser
{
	[TestMethod]
	public async Task When_Invalid_Element_Property()
	{
		var xamlFiles = new[]
		{
			new XamlFile(
				"MainPage.xaml",
				"""
				<Page x:Class="TestRepro.MainPage"
					  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					  xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">

					<Grid>

						<NavigationView IsBackButtonVisible="Collapsed"
										IsPaneToggleButtonVisible="False"
										IsSettingsVisible="False"
										PaneDisplayMode="Left">

							<NavigationView.PaneTitle FontSize="16"
													  FontWeight="Bold"
													  Text="PaneTitle" />

						</NavigationView>

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

		test.ExpectedDiagnostics.AddRange(
			new[] {
				// /0/MainPage.xaml(13,5): error UXAML0001: Member 'PaneTitle' cannot have properties at line 13, position 5
				DiagnosticResult.CompilerError("UXAML0001").WithSpan("C:/Project/0/MainPage.xaml", 13, 5, 13, 5).WithArguments("Member 'PaneTitle' cannot have properties at line 13, position 5"),
				// /0/Test0.cs(9,9): error CS1061: 'MainPage' does not contain a definition for 'InitializeComponent' and no accessible extension method 'InitializeComponent' accepting a first argument of type 'MainPage' could be found (are you missing a using directive or an assembly reference?)
				DiagnosticResult.CompilerError("CS1061").WithSpan(9, 9, 9, 28).WithArguments("TestRepro.MainPage", "InitializeComponent")
			}
		);
		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Namespace_Is_On_Nested_Element()
	{
		var xamlFiles = new[]
		{
			new XamlFile(
				"MainPage.xaml",
				"""
				<Page x:Class="TestRepro.MainPage"
					  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					  xmlns:local="using:TestRepro"
					  xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">

					<Grid>

						<local:MyStackPanel xmlns:SUT="using:NamespaceUnderTest" Source="SUT:C" />

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
					using System;
					using Windows.UI.Xaml.Controls;

					namespace TestRepro
					{

						public sealed partial class MyStackPanel : StackPanel
						{
							public Type Source { get; set; }
						}

						public sealed partial class MainPage : Page
						{
							public MainPage()
							{
								this.InitializeComponent();
							}
						}
					}

					namespace NamespaceUnderTest
					{
						public class C { }
					}
					"""
				}
			}
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Attached_DP_Interface_Type()
	{
		var xamlFile = new XamlFile(
			"MainPage.xaml",
			"""
			<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:local="using:TestRepro"
					xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">

				<Grid>
					<local:MyAttached.MyProperty>
						<local:TestDisposable />
					</local:MyAttached.MyProperty>
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
					using System;
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

						internal class TestDisposable : ITestInterface { }

						internal static class MyAttached
						{
							public static ITestInterface GetMyProperty(DependencyObject obj)
							{
								return (ITestInterface)obj.GetValue(MyPropertyProperty);
							}

							public static void SetMyProperty(DependencyObject obj, ITestInterface value)
							{
								obj.SetValue(MyPropertyProperty, value);
							}

							public static readonly DependencyProperty MyPropertyProperty =
								DependencyProperty.RegisterAttached("MyProperty", typeof(ITestInterface), typeof(MyAttached), new PropertyMetadata(null));
						}


						interface ITestInterface { }
					
					}
					"""
				}
			}
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Infinity()
	{
		var xamlFile = new XamlFile(
			"MainPage.xaml",
			"""
			<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:local="using:TestRepro"
					xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">

				<local:MyGrid MyProperty="Infinity">
					<local:MyGrid MyProperty="Infinity" />
				</local:MyGrid>
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

						public partial class MyGrid : Grid
						{
							public double MyProperty { get; set; }
						}
					}
					"""
				}
			}
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Nested_Setters()
	{
		var xamlFile = new XamlFile(
			"MainPage.xaml",
				"""
				<Page x:Class="TestRepro.MainPage"
						xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
						xmlns:local="using:TestRepro"
						xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">

					<Page.Resources>
						<Style x:Key="MyStyle" TargetType="local:MyGrid">
							<Setter Property="FirstStyle">
								<Setter.Value>
									<Style TargetType="Button" />
								</Setter.Value>
							</Setter>
							<Setter Property="Second" Value="Hello" />
						</Style>
					</Page.Resources>
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

						public partial class MyGrid : Grid
						{
							public static DependencyProperty FirstStyleProperty => throw null;
							public Style FirstStyle { get; set; }

							public static DependencyProperty SecondProperty => throw null;
							public string Second { get; set; }
						}
					}
					"""
				}
			}
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Skia_Xml_Namespace()
	{
		var xamlFile = new XamlFile(
			"MainPage.xaml",
				"""
				<Page x:Class="TestRepro.MainPage"
						xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
						xmlns:skia="using:SkiaSharp.Views.Windows"
						xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">
					<!-- This is NOT conditional XAML -->
					<skia:SKXamlCanvas />
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
			},
			DisableBuildReferences = true,
			ReferenceAssemblies = ReferenceAssemblies.Net.Net80Android.AddPackages([new PackageIdentity("SkiaSharp.Views.Uno.WinUI", "3.0.0-preview.3.1")]),
		}.AddGeneratedSources();

		await test.RunAsync();
	}
}
