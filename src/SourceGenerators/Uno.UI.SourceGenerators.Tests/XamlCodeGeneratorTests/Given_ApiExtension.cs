using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_ApiExtension
{
	private const string AppXaml = """
		<Application
			x:Class="TestRepro.App"
			xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
		</Application>
		""";

	private const string AppCodeBehind = """
		namespace TestRepro
		{
			public partial class App : global::Microsoft.UI.Xaml.Application
			{
				public App()
				{
					this.InitializeComponent();
				}
			}
		}
		""";

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25010")]
	public async Task When_Referenced_ApiExtension_Types_Are_Internal()
	{
		var test = CreateTest("internal");

		// Emitting the registration would name the types in the application's code and fail with CS0122.
		test.ExpectedDiagnostics.Add(
			new DiagnosticResult("UXAML0008", DiagnosticSeverity.Warning)
				.WithArguments("Lib.IMyExtension", "Lib", "Lib.IMyExtension"));

		await test.RunAsync();
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25010")]
	public async Task When_Referenced_ApiExtension_Types_Are_Public()
	{
		await CreateTest("public").RunAsync();
	}

	private static Verify.Test CreateTest(string accessibility)
	{
		var test = new Verify.Test(new XamlFile("App.xaml", AppXaml))
		{
			TestState =
			{
				Sources = { AppCodeBehind },
				AdditionalProjectReferences = { "Lib" },
			},
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,

			// Its output is not part of these tests, and the provider it generates is referenced by the App.
			GlobalConfigOverride = new() { ["build_property.UnoDisableBindableTypeProvidersGeneration"] = "true" },
		};

		var lib = test.TestState.AdditionalProjects["Lib"];
		lib.Sources.Add($$"""
			[assembly: global::Uno.Foundation.Extensibility.ApiExtension(typeof(Lib.IMyExtension), typeof(Lib.MyExtension))]

			namespace Lib
			{
				{{accessibility}} interface IMyExtension
				{
				}

				{{accessibility}} sealed class MyExtension : IMyExtension
				{
					public MyExtension(object owner)
					{
					}
				}
			}
			""");
		lib.AdditionalReferences.AddRange(UnoAssemblyHelper.LoadAssemblies());

		// The XAML generator also runs on the library, and requires a project path to do so.
		lib.AnalyzerConfigFiles.Add(("/Lib/.globalconfig", """
			is_global = true
			build_property.MSBuildProjectFullPath = //Lib/0/Lib.csproj
			build_property.RootNamespace = Lib
			"""));

		return test;
	}
}
