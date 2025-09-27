#if DEBUG
// Uncomment the following line to write expected files to disk
// Don't commit this line uncommented.
#define WRITE_EXPECTED
#endif

#if IS_CI && WRITE_EXPECTED
#error "WRITE_EXPECTED should not be defined!"
#endif

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.Mvvm.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.MetadataUpdates;
using Uno.UI.SourceGenerators.XamlGenerator;

namespace Uno.UI.SourceGenerators.Tests.Verifiers
{
	public record struct XamlFile(string FileName, string Contents);

	public record struct ResourceFile(string Locale, string FileName, string Contents);

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
		public static async Task AssertXamlGenerator(TestSetup testSetup, [CallerFilePath] string testFilePath = "", [CallerMemberName] string testMethodName = "")
		{
			var projectFolder = Path.GetFullPath(Path.Combine("..", "..", ".."));
			var solutionFolder = Path.GetFullPath(Path.Combine(projectFolder, "..", ".."));
			var folder = Path.GetFullPath(Path.Combine(solutionFolder, testSetup.SubFolder));
			var xaml = File.ReadAllText(Path.Combine(folder, testSetup.XamlFileName));
			var cs = File.ReadAllText(Path.Combine(folder, testSetup.XamlFileName + ".cs"));

			var test = new Test(new XamlFile(testSetup.XamlFileName, xaml), testFilePath, testMethodName)
			{
				TestState =
				{
					Sources = { cs },
				},
				PreprocessorSymbols = testSetup.PreprocessorSymbols,
			}.AddGeneratedSources();
			test.ExpectedDiagnostics.AddRange(testSetup.ExpectedDiagnostics);

			await test.RunAsync();
		}

		public class Test : TestBase
		{
			public Test(XamlFile xamlFile, [CallerFilePath] string testFilePath = "", [CallerMemberName] string testMethodName = "")
				: base(new[] { xamlFile }, testFilePath, ShortName(testMethodName)) // We use only upper-cased char to reduce length of filename push to git)
			{
			}

			public Test(XamlFile[] xamlFiles, [CallerFilePath] string testFilePath = "", [CallerMemberName] string testMethodName = "")
				: base(xamlFiles, testFilePath, ShortName(testMethodName))
			{
			}

			public Test(XamlFile[] xamlFiles, ResourceFile[] resourceFiles, [CallerFilePath] string testFilePath = "", [CallerMemberName] string testMethodName = "")
				: base(xamlFiles, resourceFiles, testFilePath, ShortName(testMethodName))
			{
			}

			private static string ShortName(string name)
				=> new string(name.Where(char.IsUpper).ToArray()); // We use only upper-cased char to reduce length of filename push to git
		}

		public abstract class TestBase : CSharpSourceGeneratorVerifier<XamlCodeGenerator>.Test
		{
			private readonly string _testFilePath;
			private readonly string _testMethodName;
			private const string TestOutputFolderName = "Out";

			private readonly XamlFile[] _xamlFiles;
			private readonly ResourceFile[] _resourceFiles;

			public bool EnableFuzzyMatching { get; set; } = true;
			public Dictionary<string, string>? GlobalConfigOverride { get; set; }

			protected TestBase(XamlFile xamlFile, [CallerFilePath] string testFilePath = "", [CallerMemberName] string testMethodName = "")
				: this([xamlFile], testFilePath, testMethodName)
			{
			}

			protected TestBase(XamlFile[] xamlFiles, string testFilePath, string testMethodName)
				: this(xamlFiles, [], testFilePath, testMethodName)
			{
			}

			protected TestBase(XamlFile[] xamlFiles, ResourceFile[] resourceFiles, string testFilePath, string testMethodName)
			{
				_xamlFiles = xamlFiles;
				_resourceFiles = resourceFiles;
				_testFilePath = testFilePath;
				_testMethodName = testMethodName;

				ReferenceAssemblies = _Dotnet.Current.ReferenceAssemblies;

#if WRITE_EXPECTED
				TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;
#endif
			}

			protected override async Task RunImplAsync(CancellationToken cancellationToken)
			{
				string? includeXamlNamespaces = null;
				string? excludeXamlNamespaces = null;
				if (ReferenceAssemblies.Packages.Any(p => p.Id.StartsWith("Microsoft.Android.Ref", StringComparison.OrdinalIgnoreCase)))
				{
					includeXamlNamespaces = "android,not_ios,not_wasm,not_macos,not_skia,not_netstdref";
					excludeXamlNamespaces = "ios,wasm,macos,skia,not_android";
				}
				else if (ReferenceAssemblies.Packages.Any(p =>
					p.Id.StartsWith("Microsoft.iOS.Ref", StringComparison.OrdinalIgnoreCase) ||
					p.Id.StartsWith("Microsoft.tvOS.Ref", StringComparison.OrdinalIgnoreCase) ||
					p.Id.StartsWith("Microsoft.MacCatalyst.Ref", StringComparison.OrdinalIgnoreCase)))
				{
					includeXamlNamespaces = "ios,not_android,not_wasm,not_macos,not_skia,not_netstdref";
					excludeXamlNamespaces = "android,wasm,macos,skia,not_ios";
				}
				else if (ReferenceAssemblies.Packages.Any(p => p.Id.StartsWith("Microsoft.macOS.Ref", StringComparison.OrdinalIgnoreCase)))
				{
					includeXamlNamespaces = "macos,not_android,not_wasm,not_ios,not_skia,not_netstdref";
					excludeXamlNamespaces = "android,ios,wasm,skia,not_macos";
				}

				var defaultConfig = new Dictionary<string, string>
				{
					{ "is_global", "true" },
					{ "build_property.MSBuildProjectFullPath", "C:\\Project\\Project.csproj" },
					{ "build_property.RootNamespace", "MyProject" },
					{ "build_property.UnoForceHotReloadCodeGen", "false" },
					{ "build_property.UnoEnableXamlFuzzyMatching", "false" },
				};

				if (includeXamlNamespaces is not null)
				{
					defaultConfig.Add("build_property.IncludeXamlNamespacesProperty", includeXamlNamespaces);
				}

				if (excludeXamlNamespaces is not null)
				{
					defaultConfig.Add("build_property.ExcludeXamlNamespacesProperty", excludeXamlNamespaces);
				}

				var globalConfigOverride = GlobalConfigOverride;
				if (globalConfigOverride is null)
				{
					globalConfigOverride = new Dictionary<string, string>();
				}

				var globalConfigBuilder = new StringBuilder();

				foreach (var (key, value) in defaultConfig)
				{
					if (!globalConfigOverride.ContainsKey(key))
					{
						globalConfigBuilder.AppendLine($"{key} = {value}");
					}
				}

				foreach (var (key, value) in globalConfigOverride)
				{
					globalConfigBuilder.AppendLine($"{key} = {value}");
				}

				globalConfigBuilder.AppendLine();

				foreach (var xamlFile in _xamlFiles)
				{
					globalConfigBuilder.Append($@"[C:/Project/0/{xamlFile.FileName}]
build_metadata.AdditionalFiles.SourceItemGroup = Page
");
					TestState.AdditionalFiles.Add(($"C:/Project/0/{xamlFile.FileName}", xamlFile.Contents));
				}

				foreach (var resourceFile in _resourceFiles)
				{
					globalConfigBuilder.Append($@"[C:/Project/0/Strings/{resourceFile.Locale}/{resourceFile.FileName}]
build_metadata.AdditionalFiles.SourceItemGroup = PRIResource
");
					TestState.AdditionalFiles.Add(($"C:/Project/0/Strings/{resourceFile.Locale}/{resourceFile.FileName}", resourceFile.Contents));
				}

				TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfigBuilder.ToString()));
				await base.RunImplAsync(cancellationToken);
			}

			public IEnumerable<string> PreprocessorSymbols { get; set; } = ImmutableArray<string>.Empty;

			protected override ParseOptions CreateParseOptions()
			{
				var options = (CSharpParseOptions)base.CreateParseOptions();
				return options.WithPreprocessorSymbols(PreprocessorSymbols);
			}

			protected override Project ApplyCompilationOptions(Project project)
			{
				project = project
					.AddMetadataReferences(UnoAssemblyHelper.LoadAssemblies());

				return base.ApplyCompilationOptions(project);
			}

			protected override async Task<(Compilation compilation, ImmutableArray<Diagnostic> generatorDiagnostics)> GetProjectCompilationAsync(Project project, IVerifier verifier, CancellationToken cancellationToken)
			{
				var resourceDirectory = Path.Combine(Path.GetDirectoryName(_testFilePath)!, TestOutputFolderName, _testMethodName);

				var (compilation, generatorDiagnostics) = await base.GetProjectCompilationAsync(project, verifier, cancellationToken);
				var expectedNames = new HashSet<string>();
				foreach (var tree in compilation.SyntaxTrees.Skip(project.DocumentIds.Count))
				{
					WriteTreeToDiskIfNecessary(tree, resourceDirectory);
					expectedNames.Add(GetFileNameFromTree(tree));
				}

				var currentTestPrefix = $"Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests.{TestOutputFolderName}.{_testMethodName}.";
				foreach (var name in GetType().Assembly.GetManifestResourceNames())
				{
					if (!name.StartsWith(currentTestPrefix))
					{
						continue;
					}

					if (!expectedNames.Contains(name.Substring(currentTestPrefix.Length)))
					{
						throw new InvalidOperationException($"Unexpected test resource: {name.Substring(currentTestPrefix.Length)}");
					}
				}

				return (compilation, generatorDiagnostics);
			}

			public TestBase AddGeneratedSources()
			{
				var expectedPrefix = $"Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests.{TestOutputFolderName}.{_testMethodName}.";
				foreach (var resourceName in typeof(Test).Assembly.GetManifestResourceNames())
				{
					if (!resourceName.StartsWith(expectedPrefix))
					{
						continue;
					}

					using var resourceStream = GetType().Assembly.GetManifestResourceStream(resourceName);
					if (resourceStream is null)
					{
						throw new InvalidOperationException();
					}

					using var reader = new StreamReader(resourceStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
					var name = resourceName.Substring(expectedPrefix.Length);
					var underscoreIndex = name.IndexOf('_');
					var generatorName = name.Substring(0, underscoreIndex);
					name = name.Substring(underscoreIndex + 1);

					var type = generatorName switch
					{
						"XamlCodeGenerator" => typeof(XamlCodeGenerator),
						"ObservablePropertyGenerator" => typeof(ObservablePropertyGenerator),
						_ => throw new Exception("Unexpected generator name"),
					};
					TestState.GeneratedSources.Add((type, name, reader.ReadToEnd()));
				}

				return this;
			}

			private static string GetFileNameFromTree(SyntaxTree tree)
			{
				var generatorName = new DirectoryInfo(tree.FilePath).Parent!.Name;
				generatorName = generatorName.Substring(generatorName.LastIndexOf('.') + 1);
				return $"{generatorName}_{Path.GetFileName(tree.FilePath)}";
			}

			[Conditional("WRITE_EXPECTED")]
			private static void WriteTreeToDiskIfNecessary(SyntaxTree tree, string resourceDirectory)
			{
				if (tree.Encoding is null)
				{
					throw new ArgumentException("Syntax tree encoding was not specified");
				}

				var name = GetFileNameFromTree(tree);

				var filePath = Path.Combine(resourceDirectory, name);
				Directory.CreateDirectory(resourceDirectory);
				File.WriteAllText(filePath, tree.GetText().ToString(), tree.Encoding);
			}
		}
	}
}
