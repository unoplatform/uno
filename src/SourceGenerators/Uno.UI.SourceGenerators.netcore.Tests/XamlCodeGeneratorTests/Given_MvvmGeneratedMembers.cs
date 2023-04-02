using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests;


[TestClass]
public class Given_MvvmGeneratedMembers
{
	private class Test : XamlSourceGeneratorVerifier.Test
	{
		private static readonly ImmutableArray<PackageIdentity> s_mvvmPackages = ImmutableArray.Create(new PackageIdentity("CommunityToolkit.Mvvm", "8.1.0"));

		public Test(XamlFile xamlFile, [CallerFilePath] string testFilePath = "", [CallerMemberName] string testMethodName = "") : base(xamlFile, testFilePath, testMethodName)
		{
			ReferenceAssemblies = ReferenceAssemblies.AddPackages(s_mvvmPackages);
		}

		public Test(XamlFile[] xamlFiles, [CallerFilePath] string testFilePath = "", [CallerMemberName] string testMethodName = "") : base(xamlFiles, testFilePath, testMethodName)
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

		var test = new Test(xamlFile, testMethodName: $"{nameof(When_ObservableProperty_AttributeExists)}_{fieldName}")
		{
			TestState =
			{
				Sources =
				{
					$$"""
					using Windows.UI.Xaml.Controls;
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

		var test = new Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					$$"""
					using Windows.UI.Xaml.Controls;
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
			// Uno.UI.SourceGenerators\Uno.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator\MainPage_d6cd66944958ced0c513e0a04797b51d.cs(75,239): error CS1061: 'MyViewModel' does not contain a definition for 'Name' and no accessible extension method 'Name' accepting a first argument of type 'MyViewModel' could be found (are you missing a using directive or an assembly reference?)
			DiagnosticResult.CompilerError("CS1061").WithSpan(@"Uno.UI.SourceGenerators\Uno.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator\MainPage_d6cd66944958ced0c513e0a04797b51d.cs", 75, 239, 75, 243).WithArguments("TestRepro.MyViewModel", "Name"),
			// Uno.UI.SourceGenerators\Uno.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator\MainPage_d6cd66944958ced0c513e0a04797b51d.cs(97,239): error CS1061: 'MyViewModel' does not contain a definition for 'Name' and no accessible extension method 'Name' accepting a first argument of type 'MyViewModel' could be found (are you missing a using directive or an assembly reference?)
			DiagnosticResult.CompilerError("CS1061").WithSpan(@"Uno.UI.SourceGenerators\Uno.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator\MainPage_d6cd66944958ced0c513e0a04797b51d.cs", 97, 239, 97, 243).WithArguments("TestRepro.MyViewModel", "Name"),
			// Uno.UI.SourceGenerators\Uno.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator\MainPage_d6cd66944958ced0c513e0a04797b51d.cs(119,239): error CS1061: 'MyViewModel' does not contain a definition for 'Name' and no accessible extension method 'Name' accepting a first argument of type 'MyViewModel' could be found (are you missing a using directive or an assembly reference?)
			DiagnosticResult.CompilerError("CS1061").WithSpan(@"Uno.UI.SourceGenerators\Uno.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator\MainPage_d6cd66944958ced0c513e0a04797b51d.cs", 119, 239, 119, 243).WithArguments("TestRepro.MyViewModel", "Name"),
		});

		await test.RunAsync();
	}
}
