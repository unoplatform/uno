using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.Windows_UI_Xaml_Controls.ParserTests;

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_Parser
{
	private static string EmptyCodeBehind(string className) =>
		$$"""
		using Microsoft.UI.Xaml.Controls;

		namespace TestRepro
		{
			public sealed partial class {{className}} : Page
			{
				public {{className}}()
				{
					this.InitializeComponent();
				}
			}
		}
		""";

	private static readonly string _emptyCodeBehind = EmptyCodeBehind("MainPage");

	[TestMethod]
	public async Task When_Empty_Xml()
	{
		var xamlFiles = new[]
		{
			new XamlFile("MainPage.xaml", ""),
		};

		var test = new Verify.Test(xamlFiles) { TestState = { Sources = { _emptyCodeBehind } } }.AddGeneratedSources();

		test.ExpectedDiagnostics.AddRange([
			DiagnosticResult.CompilerError("UXAML0001").WithArguments("Root element is missing."),
			DiagnosticResult.CompilerError("CS1061").WithSpan(9, 9, 9, 28).WithArguments("TestRepro.MainPage", "InitializeComponent"),
			DiagnosticResult.CompilerError("UXAML0001").WithSpan("C:/Project/0/MainPage.xaml", 1, 1, 1, 1).WithArguments("Failed to parse file"),
		]);

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Invalid_Xml()
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
				</Page>
				"""),
		};

		var test = new Verify.Test(xamlFiles) { TestState = { Sources = { _emptyCodeBehind } } }.AddGeneratedSources();

		test.ExpectedDiagnostics.AddRange([
			DiagnosticResult.CompilerError("UXAML0001").WithSpan("C:/Project/0/MainPage.xaml", 7, 3, 7, 3).WithArguments("The 'Grid' start tag on line 6 position 3 does not match the end tag of 'Page'. Line 7, position 3.")
			// ==> Note, even with invalid XAML, we still generate the class structure, so we should not miss `InitializeComponent`!
		]);

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Invalid_Root()
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
				"""),
		};

		var test = new Verify.Test(xamlFiles) { TestState = { Sources = { _emptyCodeBehind } } }.AddGeneratedSources();

		test.ExpectedDiagnostics.AddRange([
			DiagnosticResult.CompilerError("UXAML0001").WithSpan("C:/Project/0/MainPage.xaml", 4, 75, 4, 75).WithArguments("Unexpected end of file has occurred. The following elements are not closed: Page. Line 4, position 75.")
			// ==> Note, even with invalid XAML, we still generate the class structure, so we should not miss `InitializeComponent`!
		]);

		await test.RunAsync();
	}

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

		var test = new Verify.Test(xamlFiles) { TestState = { Sources = { _emptyCodeBehind } } }.AddGeneratedSources();

		test.ExpectedDiagnostics.AddRange([
			DiagnosticResult.CompilerError("UXAML0001").WithSpan("C:/Project/0/MainPage.xaml", 13, 5, 13, 5).WithArguments("Member 'PaneTitle' cannot have properties [Line: 13 Position: 5]")
			// ==> When XAML is invalid, we still generate the class structure, so we should not miss InitializeComponent.
		]);

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Invalid_Member_Node()
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
						<Grid.InvalidMember>
							<Border />
						</Grid.InvalidMember>
					</Grid>
				</Page>
				"""),
		};

		var test = new Verify.Test(xamlFiles) { TestState = { Sources = { _emptyCodeBehind } } }.AddGeneratedSources();

		test.ExpectedDiagnostics.AddRange([
			DiagnosticResult.CompilerError("CS0117").WithSpan(@"Uno.UI.SourceGenerators\Uno.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator\MainPage_d6cd66944958ced0c513e0a04797b51d.cs", 56, 5, 56, 18).WithArguments("Microsoft.UI.Xaml.Controls.Grid", "InvalidMember")
			// ==> When XAML is invalid, we still generate the class structure, so we should not miss InitializeComponent.
		]);

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Invalid_Object_Node()
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
						<TypeThatDoesNotExist />
					</Grid>
				</Page>
				"""),
		};

		var test = new Verify.Test(xamlFiles) { TestState = { Sources = { _emptyCodeBehind } } }.AddGeneratedSources();

		test.ExpectedDiagnostics.AddRange([
			DiagnosticResult.CompilerError("CS0246").WithSpan(@"Uno.UI.SourceGenerators\Uno.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator\MainPage_d6cd66944958ced0c513e0a04797b51d.cs", 59, 10, 59, 30).WithArguments("TypeThatDoesNotExist"),
			// ==> When XAML is invalid, we still generate the class structure, so we should not miss InitializeComponent.
		]);

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Invalid_Object_Property()
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

					<Grid PropertyThatDoesNotExist="5">
					</Grid>
				</Page>
				"""),
		};

		var test = new Verify.Test(xamlFiles) { TestState = { Sources = { _emptyCodeBehind } } }.AddGeneratedSources();

		test.ExpectedDiagnostics.AddRange([
			DiagnosticResult.CompilerError("CS1029").WithSpan(@"Uno.UI.SourceGenerators\Uno.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator\MainPage_d6cd66944958ced0c513e0a04797b51d.cs", 57, 12, 57, 239).WithArguments("Property PropertyThatDoesNotExist does not exist on {http://schemas.microsoft.com/winfx/2006/xaml/presentation}Grid, name is PropertyThatDoesNotExist, preferrednamespace http://schemas.microsoft.com/winfx/2006/xaml/presentation"),
			// ==> When XAML is invalid, we still generate the class structure, so we should not miss InitializeComponent.
		]);

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Multiple_With_Invalid()
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

					<Grid />
				</Page>
				"""),
			new XamlFile(
				"SecondPage.xaml",
				"""
				<Page x:Class="TestRepro.SecondPage"
					  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					  xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">

					<Grid PropertyThatDoesNotExist="5">
					</Grid>
				</Page>
				"""),
			new XamlFile(
				"ThirdPage.xaml",
				"""
				<Page x:Class="TestRepro.ThirdPage"
					  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					  xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">

					<Grid>
				</Page>
				"""),
		};

		var test = new Verify.Test(xamlFiles) { TestState = { Sources = { EmptyCodeBehind("MainPage"), EmptyCodeBehind("SecondPage"), EmptyCodeBehind("ThirdPage") } } }.AddGeneratedSources();

		test.ExpectedDiagnostics.AddRange([
			DiagnosticResult.CompilerError("UXAML0001").WithSpan("C:/Project/0/ThirdPage.xaml", 7, 3, 7, 3).WithArguments("The 'Grid' start tag on line 6 position 3 does not match the end tag of 'Page'. Line 7, position 3."),
			DiagnosticResult.CompilerError("CS1029").WithSpan(@"Uno.UI.SourceGenerators\Uno.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator\SecondPage_0109051836b2d11a4ba3400a576defb2.cs", 57, 12, 57, 239).WithArguments("Property PropertyThatDoesNotExist does not exist on {http://schemas.microsoft.com/winfx/2006/xaml/presentation}Grid, name is PropertyThatDoesNotExist, preferrednamespace http://schemas.microsoft.com/winfx/2006/xaml/presentation"),
			// ==> When XAML is invalid, we still generate the class structure, so we should not miss InitializeComponent.
		]);

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
					using Microsoft.UI.Xaml.Controls;

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
			ReferenceAssemblies = _Dotnet.CurrentAndroid.ReferenceAssemblies.AddPackages([new PackageIdentity("SkiaSharp.Views.Uno.WinUI", "3.0.0-preview.3.1")]),
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Event_On_TopLevel_Not_DependencyObject()
	{
		var xamlFile = new XamlFile(
			"MainWindow.xaml",
				"""
				<Window x:Class="TestRepro.MainWindow"
						xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
						xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
						Closed="Window_Closed">
					
				</Window>
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
						public sealed partial class MainWindow : Window
						{
							public MainWindow()
							{
								this.InitializeComponent();
							}

							private void Window_Closed(object sender, WindowEventArgs args) { }
						}
					}
					"""
				}
			},
			ReferenceAssemblies = _Dotnet.Current.WithUnoPackage("5.3.114"),
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Vector_Properties()
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

						<local:MyStackPanel Vec2="1,2" Vec3="3,4,5" />

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
					using Microsoft.UI.Xaml.Controls;

					namespace TestRepro
					{

						public sealed partial class MyStackPanel : StackPanel
						{
							public global::System.Numerics.Vector2 Vec2 { get; set; }

							public global::System.Numerics.Vector3 Vec3 { get; set; }
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
