using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.XamlGenerator;

namespace Uno.UI.SourceGenerators.Tests.Verifiers
{
	public record struct XamlFile(string FileName, string Contents);

	public class TestSetup
	{
		public TestSetup(string xamlFileName, string subFolder)
		{
			XamlFileName = xamlFileName;
			SubFolder = subFolder;
		}

		public string XamlFileName { get; }
		public string SubFolder { get; }
		public List<string> PreprocessorSymbols { get; } = new List<string>();
		public List<DiagnosticResult> ExpectedDiagnostics { get; } = new List<DiagnosticResult>();
	}

	public static partial class XamlSourceGeneratorVerifier
	{
		public static async Task AssertXamlGeneratorDiagnostics(TestSetup testSetup)
		{
			var projectFolder = Path.GetFullPath(Path.Combine("..", "..", ".."));
			var solutionFolder = Path.GetFullPath(Path.Combine(projectFolder, "..", ".."));
			var folder = Path.GetFullPath(Path.Combine(solutionFolder, testSetup.SubFolder));
			var xaml = File.ReadAllText(Path.Combine(folder, testSetup.XamlFileName));
			var cs = File.ReadAllText(Path.Combine(folder, testSetup.XamlFileName + ".cs"));

			var test = new Test(new XamlFile(testSetup.XamlFileName, xaml))
			{
				TestState =
				{
					Sources = { cs },
				},
				PreprocessorSymbols = testSetup.PreprocessorSymbols,
			};
			test.ExpectedDiagnostics.AddRange(testSetup.ExpectedDiagnostics);

			await test.RunAsync();
		}

		public class Test : CSharpSourceGeneratorVerifier<XamlCodeGenerator>.Test
		{
			public Test(XamlFile xamlFile, string globalConfig = "")
				: this(new[] { xamlFile }, globalConfig)
			{
			}

			public Test(XamlFile[] xamlFiles, string globalConfig = "")
			{
				var globalConfigBuilder = new StringBuilder(@"
is_global = true
# For now, there is no need to customize these for each test.
build_property.MSBuildProjectFullPath = C:\Project\Project.csproj
build_property.RootNamespace = MyProject
");
				globalConfigBuilder.AppendLine(globalConfig);

				foreach (var xamlFile in xamlFiles)
				{
					globalConfigBuilder.Append($@"[/0/{xamlFile.FileName}]
build_metadata.AdditionalFiles.SourceItemGroup = Page
");
					TestState.AdditionalFiles.Add(($"/0/{xamlFile.FileName}", xamlFile.Contents));
				}
				TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfigBuilder.ToString()));
				ReferenceAssemblies = ReferenceAssemblies.Default.AddPackages(ImmutableArray.Create(new PackageIdentity("Uno.UI", "4.4.20")));
				TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck;
			}

			public IEnumerable<string> PreprocessorSymbols { get; set; } = ImmutableArray<string>.Empty;

			protected override ParseOptions CreateParseOptions()
			{
				var options = (CSharpParseOptions)base.CreateParseOptions();
				return options.WithPreprocessorSymbols(PreprocessorSymbols);
				
			}
		}
	}
}
