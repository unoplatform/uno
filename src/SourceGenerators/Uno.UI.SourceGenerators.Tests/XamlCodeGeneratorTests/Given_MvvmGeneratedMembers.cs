using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests;

// https://github.com/unoplatform/uno/issues/12073


[TestClass]
public class Given_MvvmGeneratedMembers
{
	private class MvvmTest : XamlSourceGeneratorVerifier.TestBase
	{
		private static readonly ImmutableArray<PackageIdentity> s_mvvmPackages = ImmutableArray.Create(new PackageIdentity("CommunityToolkit.Mvvm", "8.2.0"));

		public MvvmTest(XamlFile xamlFile, string testMethodName, [CallerFilePath] string testFilePath = "") : base(xamlFile, testFilePath, testMethodName)
		{
			ReferenceAssemblies = ReferenceAssemblies.AddPackages(s_mvvmPackages);
		}

		public MvvmTest(XamlFile[] xamlFiles, string testMethodName, [CallerFilePath] string testFilePath = "") : base(xamlFiles, testFilePath, testMethodName)
		{
			ReferenceAssemblies = ReferenceAssemblies.AddPackages(s_mvvmPackages);
		}

		protected override IEnumerable<Type> GetSourceGenerators()
		{
			foreach (var generatorType in base.GetSourceGenerators())
			{
				yield return generatorType;
			}

			yield return typeof(ObservablePropertyGenerator);
			yield return typeof(RelayCommandGenerator);
		}
	}

	[TestMethod]
	[DataRow("name")]
	[DataRow("_name")]
	[DataRow("m_name")]
	public async Task When_ObservableProperty_AttributeExists(string fieldName)
	{
		var xamlFile = new XamlFile(
			"MainPage.xaml",
			"""
			<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">

				<StackPanel>
					<TextBlock Text="{x:Bind ViewModel.Name.ToUpper()}" />
					<TextBlock Text="{x:Bind ViewModel.Name.ToUpper(), Mode=OneWay}" />
					<TextBlock Text="{x:Bind ViewModel.Name.ToUpper(), Mode=TwoWay, BindBack=ViewModel.MyBindBack}" />
				</StackPanel>
			</Page>
			""");

		var test = new MvvmTest(xamlFile, $"WOPAE_{fieldName}")
		{
			TestState =
			{
				Sources =
				{
					$$"""
					using Microsoft.UI.Xaml.Controls;
					using CommunityToolkit.Mvvm.ComponentModel;

					namespace TestRepro
					{
						public sealed partial class MainPage : Page
						{
							public MyViewModel ViewModel = new MyViewModel();

							public MainPage()
							{
								this.InitializeComponent();
							}
						}

						public partial class MyViewModel : ObservableObject
						{
							[ObservableProperty]
							private string {{fieldName}};

							public void MyBindBack(string s) { }
						}
					}
					"""
				}
			}
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Boolean_Observable_Property()
	{
		var xamlFile = new XamlFile(
			"MainPage.xaml",
			"""
			<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">

				<StackPanel>
					<ToggleSwitch IsOn="{x:Bind ViewModel.IsEnabled, Mode=TwoWay}" OnContent="Enabled" OffContent="Disabled"/>
				</StackPanel>
			</Page>
			""");

		var test = new MvvmTest(xamlFile, $"WBOP")
		{
			TestState =
			{
				Sources =
				{
					$$"""
					using Microsoft.UI.Xaml.Controls;
					using CommunityToolkit.Mvvm.ComponentModel;

					namespace TestRepro
					{
						public sealed partial class MainPage : Page
						{
							public MyViewModel ViewModel = new MyViewModel();

							public MainPage()
							{
								this.InitializeComponent();
							}
						}

						public partial class MyViewModel : ObservableObject
						{
							[ObservableProperty]
							private bool _isEnabled;
						}
					}
					"""
				}
			}
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Nested_Boolean_Observable_Property()
	{
		var xamlFile = new XamlFile(
			"MainPage.xaml",
			"""
			<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">

				<StackPanel>
					<ToggleSwitch IsOn="{x:Bind ViewModel.SubModel.IsEnabled, Mode=TwoWay}" OnContent="Enabled" OffContent="Disabled"/>
				</StackPanel>
			</Page>
			""");

		var test = new MvvmTest(xamlFile, $"WNBOP")
		{
			TestState =
			{
				Sources =
				{
					$$"""
					using Microsoft.UI.Xaml.Controls;
					using CommunityToolkit.Mvvm.ComponentModel;

					namespace TestRepro
					{
						public sealed partial class MainPage : Page
						{
							public MyViewModel ViewModel = new MyViewModel();

							public MainPage()
							{
								this.InitializeComponent();
							}
						}

						public partial class MyViewModel : ObservableObject
						{
							[ObservableProperty]
							private MySubViewModel _subModel;
						}

						public partial class MySubViewModel : ObservableObject
						{
							[ObservableProperty]
							private bool _isEnabled;
						}
					}
					"""
				}
			}
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_ObservableProperty_AttributeDoesNotExists()
	{
		// This test is an error scenario case.
		// Despite the "name" field doesn't have ObservableProperty attribute, we still take its type into account
		// and try to bind against "Name" property which won't exist.
		var xamlFile = new XamlFile(
			"MainPage.xaml",
			"""
			<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">

				<StackPanel>
					<TextBlock Text="{x:Bind ViewModel.Name.ToUpper()}" />
					<TextBlock Text="{x:Bind ViewModel.Name.ToUpper(), Mode=OneWay}" />
					<TextBlock Text="{x:Bind ViewModel.Name.ToUpper(), Mode=TwoWay, BindBack=ViewModel.MyBindBack}" />
				</StackPanel>
			</Page>
			""");

		var test = new MvvmTest(xamlFile, "WOPADNE")
		{
			TestState =
			{
				Sources =
				{
					$$"""
					using Microsoft.UI.Xaml.Controls;
					using CommunityToolkit.Mvvm.ComponentModel;

					namespace TestRepro
					{
						public sealed partial class MainPage : Page
						{
							public MyViewModel ViewModel = new MyViewModel();

							public MainPage()
							{
								this.InitializeComponent();
							}
						}

						public partial class MyViewModel : ObservableObject
						{
							private string name;

							public void MyBindBack(string s) { }
						}
					}
					"""
				}
			}
		}.AddGeneratedSources();
		test.ExpectedDiagnostics.AddRange(new[]
		{
			// Uno.UI.SourceGenerators\Uno.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator\MainPage_d6cd66944958ced0c513e0a04797b51d.cs(76,254): error CS1061: 'MyViewModel' does not contain a definition for 'Name' and no accessible extension method 'Name' accepting a first argument of type 'MyViewModel' could be found (are you missing a using directive or an assembly reference?)
			DiagnosticResult.CompilerError("CS1061").WithSpan(Path.Combine("Uno.UI.SourceGenerators", "Uno.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator", "MainPage_d6cd66944958ced0c513e0a04797b51d.cs"), 76, 254, 76, 258).WithArguments("TestRepro.MyViewModel", "Name"),
			// Uno.UI.SourceGenerators\Uno.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator\MainPage_d6cd66944958ced0c513e0a04797b51d.cs(100,254): error CS1061: 'MyViewModel' does not contain a definition for 'Name' and no accessible extension method 'Name' accepting a first argument of type 'MyViewModel' could be found (are you missing a using directive or an assembly reference?)
			DiagnosticResult.CompilerError("CS1061").WithSpan(Path.Combine("Uno.UI.SourceGenerators", "Uno.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator", "MainPage_d6cd66944958ced0c513e0a04797b51d.cs"), 100, 254, 100, 258).WithArguments("TestRepro.MyViewModel", "Name"),
			// Uno.UI.SourceGenerators\Uno.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator\MainPage_d6cd66944958ced0c513e0a04797b51d.cs(124,252): error CS1061: 'MyViewModel' does not contain a definition for 'Name' and no accessible extension method 'Name' accepting a first argument of type 'MyViewModel' could be found (are you missing a using directive or an assembly reference?)
			DiagnosticResult.CompilerError("CS1061").WithSpan(Path.Combine("Uno.UI.SourceGenerators", "Uno.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator", "MainPage_d6cd66944958ced0c513e0a04797b51d.cs"), 124, 254, 124, 258).WithArguments("TestRepro.MyViewModel", "Name"),
		});

		await test.RunAsync();
	}

	// Reproduction for https://github.com/unoplatform/uno/issues/12073
	// x:Bind TwoWay failed to compile when the target property on the page class
	// was produced by a separate (external) source generator — OneWay worked but
	// TwoWay could not find the generated property. This simulates that scenario
	// by using a small custom source generator that adds the ViewModel property
	// to MainPage as a second partial, then verifies both OneWay and TwoWay
	// compile cleanly against it.
	private class ExternalViewModelPropertyGeneratorTest : XamlSourceGeneratorVerifier.TestBase
	{
		public ExternalViewModelPropertyGeneratorTest(XamlFile xamlFile, string testMethodName, [CallerFilePath] string testFilePath = "")
			: base(xamlFile, testFilePath, testMethodName)
		{
		}

		protected override IEnumerable<Type> GetSourceGenerators()
		{
			foreach (var generatorType in base.GetSourceGenerators())
			{
				yield return generatorType;
			}

			yield return typeof(ExternalViewModelPropertyGenerator);
		}
	}

#pragma warning disable RS1036, RS1038, RS1041, RS1042
	[Generator]
	private sealed class ExternalViewModelPropertyGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context) { }

		public void Execute(GeneratorExecutionContext context)
		{
			const string source = """
				namespace TestRepro
				{
					public partial class MyViewModel
					{
						public string Name { get; set; }
					}

					public sealed partial class MainPage
					{
						public MyViewModel ViewModel { get; set; } = new MyViewModel();
					}
				}
				""";

			context.AddSource("ExternalViewModelProperty.g.cs", source);
		}
	}
#pragma warning restore RS1036, RS1038, RS1041, RS1042

	[TestMethod]
	public async Task When_Page_Has_External_Source_Generated_ViewModel_Property_TwoWay_12073()
	{
		var xamlFile = new XamlFile(
			"MainPage.xaml",
			"""
			<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">

				<StackPanel>
					<TextBox Text="{x:Bind ViewModel.Name, Mode=TwoWay}" />
				</StackPanel>
			</Page>
			""");

		var test = new ExternalViewModelPropertyGeneratorTest(xamlFile, "WPHESGVMPT12073")
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

						public partial class MyViewModel
						{
						}
					}
					"""
				}
			}
		};
		test.TestBehaviors |= Microsoft.CodeAnalysis.Testing.TestBehaviors.SkipGeneratedSourcesCheck;

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Page_Has_External_Source_Generated_ViewModel_Property_OneWay_12073()
	{
		var xamlFile = new XamlFile(
			"MainPage.xaml",
			"""
			<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">

				<StackPanel>
					<TextBox Text="{x:Bind ViewModel.Name, Mode=OneWay}" />
				</StackPanel>
			</Page>
			""");

		var test = new ExternalViewModelPropertyGeneratorTest(xamlFile, "WPHESGVMPO12073")
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

						public partial class MyViewModel
						{
						}
					}
					"""
				}
			}
		};
		test.TestBehaviors |= Microsoft.CodeAnalysis.Testing.TestBehaviors.SkipGeneratedSourcesCheck;

		await test.RunAsync();
	}
}
