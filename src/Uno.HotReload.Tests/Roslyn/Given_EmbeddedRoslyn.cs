using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Uno.HotReload.Roslyn;
using Uno.HotReload.Tests.TestUtils;

namespace Uno.HotReload.Tests.Roslyn;

/// <summary>
/// Tests for <see cref="EmbeddedRoslyn"/>: the <c>CompilerApiVersion</c> pin forwarded to the
/// hot-reload workspace (analyzer flavor selection) and the per-project analyzer load-failure
/// reporting.
/// </summary>
[TestClass]
public sealed partial class Given_EmbeddedRoslyn
{
	[GeneratedRegex(@"^roslyn\d+\.\d+$")]
	private static partial Regex CompilerApiVersionShape();

	[TestMethod]
	public void When_CompilerApiVersion_Then_MatchesTheEmbeddedCompilationAssembly()
	{
		var version = typeof(Compilation).Assembly.GetName().Version;

		Assert.IsNotNull(version);
		StringAssert.Matches(EmbeddedRoslyn.CompilerApiVersion, CompilerApiVersionShape());
		Assert.AreEqual($"roslyn{version.Major}.{version.Minor}", EmbeddedRoslyn.CompilerApiVersion);
	}

	[TestMethod]
	public void When_CompilerApiVersion_Then_TracksThePackageLine()
	{
		// The analyzers/dotnet/roslyn{X.Y} flavor folders are named after the PACKAGE version, so
		// the pin is only correct as long as AssemblyName.Version tracks the package major.minor.
		// The informational version starts with the package version: this guards against an
		// assembly-versioning scheme change on a future Roslyn bump.
		var informational = typeof(Compilation).Assembly
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
			.InformationalVersion;
		var packageLine = string.Join('.', informational.Split('-', '+')[0].Split('.').Take(2));

		Assert.AreEqual($"roslyn{packageLine}", EmbeddedRoslyn.CompilerApiVersion);
	}

	[TestMethod]
	public void When_UnloadableAnalyzer_Then_WarnsOncePerProjectNamingIt()
	{
		// A corrupt assembly under a flavor-style folder: the load failure is silent through
		// GetGenerators() (empty array), the warning is the only signal.
		var root = Directory.CreateTempSubdirectory("uno-hr-tests").FullName;
		try
		{
			var analyzerPath = Path.Combine(root, "analyzers", "dotnet", "roslyn9.9", "cs", "BogusGenerators.dll");
			Directory.CreateDirectory(Path.GetDirectoryName(analyzerPath)!);
			File.WriteAllBytes(analyzerPath, [0xB0, 0x60, 0x05, 0x00]);
			var reference = new AnalyzerFileReference(analyzerPath, new TestAnalyzerAssemblyLoader());

			var lib1 = ProjectId.CreateNewId();
			var lib2 = ProjectId.CreateNewId();
			using var workspace = new AdhocWorkspace();
			var solution = workspace.CurrentSolution
				.AddProject(lib1, "Lib1", "Lib1", LanguageNames.CSharp)
				.AddAnalyzerReference(lib1, reference)
				.AddProject(lib2, "Lib2", "Lib2", LanguageNames.CSharp)
				.AddAnalyzerReference(lib2, reference);
			var reporter = new RecordingReporter();

			EmbeddedRoslyn.WarnOnAnalyzerLoadFailures(solution, reporter);

			Assert.AreEqual(2, reporter.Warnings.Count, string.Join(Environment.NewLine, reporter.Warnings));
			foreach (var project in (string[])["Lib1", "Lib2"])
			{
				var warning = reporter.Warnings.SingleOrDefault(w => w.Contains($"'{project}'", StringComparison.Ordinal));
				Assert.IsNotNull(warning, $"expected exactly one warning naming project '{project}'");
				StringAssert.Contains(warning, "'BogusGenerators'");
				StringAssert.Contains(warning, "(roslyn9.9)");
				StringAssert.Contains(warning, $"(Roslyn {EmbeddedRoslyn.Version.ToString(2)})");
				StringAssert.Contains(warning, "hot reload will NOT work");
			}
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	public void When_LoadableAnalyzer_Then_NoWarning()
	{
		// Any loadable managed assembly works as a no-generator analyzer reference; the test
		// assembly itself is the simplest one at hand.
		var reference = new AnalyzerFileReference(typeof(Given_EmbeddedRoslyn).Assembly.Location, new TestAnalyzerAssemblyLoader());

		var lib = ProjectId.CreateNewId();
		using var workspace = new AdhocWorkspace();
		var solution = workspace.CurrentSolution
			.AddProject(lib, "Lib1", "Lib1", LanguageNames.CSharp)
			.AddAnalyzerReference(lib, reference);
		var reporter = new RecordingReporter();

		EmbeddedRoslyn.WarnOnAnalyzerLoadFailures(solution, reporter);

		Assert.AreEqual(0, reporter.Warnings.Count, string.Join(Environment.NewLine, reporter.Warnings));
	}

	private sealed class TestAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
	{
		public void AddDependencyLocation(string fullPath)
		{
		}

		public Assembly LoadFromPath(string fullPath)
			=> Assembly.LoadFrom(fullPath);
	}
}
