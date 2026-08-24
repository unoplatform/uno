#if DEBUG
// Uncomment the following line to write expected files to disk
// Don't commit this line uncommented.
// #define WRITE_EXPECTED
// note: remember to chain `.AddGeneratedSources()` to the Verifier.Test
#endif

#if IS_CI && WRITE_EXPECTED
#error "WRITE_EXPECTED should not be defined!"
#endif

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
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

		public partial class Test : TestBase
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

			[GeneratedRegex("(?<slug>[A-Z][a-z])[a-z_]*")]
			private static partial Regex GetShortNameRegex();

			private static string ShortName(string name)
				=> GetShortNameRegex().Replace(name, "${slug}"); // We use only upper-cased char to reduce length of filename push to git
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
					includeXamlNamespaces = "android,not_ios,not_wasm,not_skia,not_netstdref";
					excludeXamlNamespaces = "ios,wasm,skia,not_android";
				}
				else if (ReferenceAssemblies.Packages.Any(p =>
					p.Id.StartsWith("Microsoft.iOS.Ref", StringComparison.OrdinalIgnoreCase) ||
					p.Id.StartsWith("Microsoft.tvOS.Ref", StringComparison.OrdinalIgnoreCase) ||
					p.Id.StartsWith("Microsoft.MacCatalyst.Ref", StringComparison.OrdinalIgnoreCase)))
				{
					includeXamlNamespaces = "ios,not_android,not_wasm,not_skia,not_netstdref";
					excludeXamlNamespaces = "android,wasm,skia,not_ios";
				}

				// The synthetic paths below start with TWO separators on purpose: that is the only shape
				// Roslyn accepts as an absolute analyzer-config section name on both platforms -- on Unix
				// the name must start with '/', on Windows it must be drive-rooted or UNC, and '//x/y' is
				// the intersection (AnalyzerConfig.IsAbsoluteEditorConfigPath). A section name that is not
				// absolute is silently dropped, taking SourceItemGroup with it, so the generator would see
				// no Page item at all.
				//
				// Windows then reads '//Project/...' as the UNC path '\\Project\...', whose ROOT is
				// '\\server\share' -- so the project file needs at least two segments above it. With
				// '//Project/Project.csproj', 'Project.csproj' IS the share: the path is its own root,
				// Path.GetDirectoryName returns null, and XamlCodeGeneration throws for an invalid
				// MSBuildProjectFullPath -- generating nothing at all, on every test.
				var defaultConfig = new Dictionary<string, string>
				{
					{ "is_global", "true" },
					{ "build_property.MSBuildProjectFullPath", "//Project/0/Project.csproj" },
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

				// The item paths are frozen: a generated file is named after a hash of the item path it
				// came from (XamlFileDefinition.UniqueID), and that name is also emitted INTO the
				// generated code, so moving the items renames and rewrites every snapshot. That is why
				// the project file above sits next to them rather than one folder up, and why Link --
				// which an item is free to declare independently of where it physically lives -- carries
				// the `0/` folder instead.
				//
				// Link is declared so the generated content is identical on every host. Without it the
				// generator derives the "source link" (the BaseUri target path and the ms-appx:/// URIs)
				// from the item path MINUS _projectDirectory, using Path.GetDirectoryName and
				// Path.DirectorySeparatorChar — both host-dependent, so the same XAML yields
				// `0\MainPage.xaml` on Windows and `0/MainPage.xaml` on Unix and no single set of
				// committed snapshots can match both. Link short-circuits that math in GetSourceLink and
				// XamlFileParser.GetTargetFilePath alike. The `// Source …` comments come from a THIRD
				// computation that Link does not reach (XamlFileGenerator._relativePath, relative to the
				// project directory), which is why the items sit directly in it: the result then holds no
				// separator at all, and cannot differ between hosts.
				foreach (var xamlFile in _xamlFiles)
				{
					globalConfigBuilder.Append($@"[//Project/0/{xamlFile.FileName}]
build_metadata.AdditionalFiles.SourceItemGroup = Page
build_metadata.AdditionalFiles.Link = 0/{xamlFile.FileName}
");
					TestState.AdditionalFiles.Add(($"//Project/0/{xamlFile.FileName}", NormalizeNewLines(xamlFile.Contents)));
				}

				foreach (var resourceFile in _resourceFiles)
				{
					globalConfigBuilder.Append($@"[//Project/0/Strings/{resourceFile.Locale}/{resourceFile.FileName}]
build_metadata.AdditionalFiles.SourceItemGroup = PRIResource
build_metadata.AdditionalFiles.Link = 0/Strings/{resourceFile.Locale}/{resourceFile.FileName}
");
					TestState.AdditionalFiles.Add(($"//Project/0/Strings/{resourceFile.Locale}/{resourceFile.FileName}", NormalizeNewLines(resourceFile.Contents)));
				}

				TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfigBuilder.ToString()));
				await base.RunImplAsync(cancellationToken);
			}

			/// <summary>
			/// Pins the newlines of a test input, so that what the generator derives from its BYTES is the
			/// same on every host.
			/// </summary>
			/// <remarks>
			/// The inputs arrive either as raw string literals in the test file or read from a checked-in
			/// file, so under the repository's <c>* text=auto</c> they carry CRLF on Windows and LF on Unix.
			/// The generated code mostly reflects the parsed XAML, which does not care — except for the
			/// embedded-sources provider, which emits a SHA-1 of the source text (XamlFileDefinition.Checksum)
			/// as a hex string. Unlike a newline, a hash cannot be converted back on checkout: unpinned, it
			/// makes the Given_GenerateEmbeddedXamlSources and Given_HotReloadEnabledInBuild snapshots match
			/// on exactly one platform.
			/// </remarks>
			private static string NormalizeNewLines(string content)
				=> content.Replace("\r\n", "\n");

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
				var resourceDirectory = Path.Combine(Path.GetDirectoryName(_testFilePath)!, TestOutputFolderName, Path.GetFileNameWithoutExtension(_testFilePath), _testMethodName);

				var (compilation, generatorDiagnostics) = await base.GetProjectCompilationAsync(project, verifier, cancellationToken);
				var expectedNames = new HashSet<string>();
				foreach (var tree in compilation.SyntaxTrees.Skip(project.DocumentIds.Count))
				{
					WriteTreeToDiskIfNecessary(tree, resourceDirectory);
					expectedNames.Add(GetFileNameFromTree(tree));
				}

				var currentTestPrefix = $"Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests.{TestOutputFolderName}.{Path.GetFileNameWithoutExtension(_testFilePath)}.{_testMethodName}.";
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

			public TestBase AddSource(string source)
			{
				TestState.Sources.Add(source);

				return this;
			}

			public TestBase AddGeneratedSources()
			{
				var expectedPrefix = $"Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests.{TestOutputFolderName}.{Path.GetFileNameWithoutExtension(_testFilePath)}.{_testMethodName}.";
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
					// Build the SourceText explicitly with Sha256 instead of handing over a string: the
					// implicit string conversion uses SourceText.From's Sha1 default, while Roslyn 5.x
					// creates the project — and hence the actual generated documents — with
					// SourceHashAlgorithms.Default = Sha256. The harness matches expected to actual
					// documents by a weighted distance over content, encoding AND checksum algorithm,
					// so a Sha1 baseline makes every pair inexact: no exact alignment exists, the
					// matcher falls back to an arbitrary one, and the failure surfaces as a confusing
					// "Expected source file list to match" rather than a checksum mismatch.
					// The checksum algorithm has to match the ACTUAL generated document per file, because the
					// harness matches expected to actual by a weighted distance over content, encoding AND
					// checksum algorithm: a single mismatched document leaves no exact alignment, the matcher
					// falls back to an arbitrary one, and the failure surfaces as a misleading "Expected
					// source file list to match". Roslyn derives the algorithm from HOW the generator adds
					// the source: AddSource(name, string) uses the compilation's algorithm (Sha256 on the
					// 5.x line), while AddSource(name, SourceText) keeps whatever the SourceText carries --
					// and every Uno/CommunityToolkit generator here builds it with SourceText.From, whose
					// API default is Sha1. LocalizationResources is the single string-overload call
					// (XamlCodeGeneration.TryGenerateUnoResourcesKeyAttribute), hence the odd one out.
					var checksumAlgorithm = name == "LocalizationResources.cs"
						? SourceHashAlgorithm.Sha256
						: SourceHashAlgorithm.Sha1;

					TestState.GeneratedSources.Add((type, name, SourceText.From(reader.ReadToEnd(), Encoding.UTF8, checksumAlgorithm)));
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
