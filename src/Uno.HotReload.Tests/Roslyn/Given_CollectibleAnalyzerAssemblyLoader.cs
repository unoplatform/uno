using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Uno.HotReload.Roslyn;
using Uno.HotReload.Tests.TestUtils;
using Uno.HotReload.Utils;

namespace Uno.HotReload.Tests.Roslyn;

/// <summary>
/// Tests for <see cref="CollectibleAnalyzerAssemblyLoader"/>: analyzers must load in COLLECTIBLE
/// contexts (so they may reference an embedded Roslyn hosted in a collectible context — Roslyn
/// 5.x's own analyzer contexts are non-collectible and fail there), through shadow copies (the
/// original file must never be locked), with Microsoft.CodeAnalysis unified to the embedded one.
/// </summary>
[TestClass]
public sealed class Given_CollectibleAnalyzerAssemblyLoader
{
	[TestMethod]
	[Description(
		"Analyzer contexts must be COLLECTIBLE (Roslyn 5.x's own are not, and die referencing " +
		"the collectible per-application context hosting the embedded Roslyn) and shared per " +
		"directory, so co-located analyzer assemblies see each other like Roslyn's loader.")]
	public void When_LoadFromPath_Then_LoadsCollectible_And_SharesContextPerDirectory()
	{
		var root = Directory.CreateTempSubdirectory("uno-hr-tests").FullName;
		try
		{
			var payload1 = CopyTo(root, typeof(Given_CollectibleAnalyzerAssemblyLoader).Assembly.Location);
			var payload2 = CopyTo(root, typeof(RecordingReporter).Assembly.Location, "Payload2.dll");
			var loader = new CollectibleAnalyzerAssemblyLoader();

			var assembly1 = loader.LoadFromPath(payload1);
			var assembly2 = loader.LoadFromPath(payload2);

			var context = AssemblyLoadContext.GetLoadContext(assembly1);
			Assert.IsNotNull(context);
			Assert.IsTrue(context.IsCollectible, "analyzer contexts must be collectible");
			Assert.AreSame(context, AssemblyLoadContext.GetLoadContext(assembly2), "same directory must share one context");
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	[Description(
		"Loading must go through a shadow copy: the analyzer file on disk belongs to the user's " +
		"build (IDE/CLI rebuilds it at will) and locking it would break those builds on Windows.")]
	public void When_LoadFromPath_Then_OriginalFileIsNotLocked()
	{
		var root = Directory.CreateTempSubdirectory("uno-hr-tests").FullName;
		try
		{
			var payload = CopyTo(root, typeof(Given_CollectibleAnalyzerAssemblyLoader).Assembly.Location);
			var loader = new CollectibleAnalyzerAssemblyLoader();

			_ = loader.LoadFromPath(payload);

			// The shadow copy is what got memory-mapped: rewriting and deleting the original must
			// both succeed (on Windows a directly-loaded assembly file would fail here).
			File.WriteAllBytes(payload, File.ReadAllBytes(payload));
			File.Delete(payload);
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	[Description(
		"THE dev-server scenario: the embedded Roslyn lives in a COLLECTIBLE per-application " +
		"context. The analyzer context must resolve compiler assemblies to that exact instance " +
		"(type identity), which Roslyn 5.x's non-collectible contexts cannot even reference.")]
	public void When_CompilerLivesInCollectibleContext_Then_AnalyzerRequestsUnifyToIt()
	{
		// THE dev-server scenario: the embedded Roslyn is hosted in a COLLECTIBLE per-application
		// context. Roslyn 5.x's non-collectible analyzer contexts cannot even reference it (the
		// runtime forbids non-collectible -> collectible references); ours must resolve compiler
		// assemblies to that exact instance.
		var root = Directory.CreateTempSubdirectory("uno-hr-tests").FullName;
		var compilerContext = new AssemblyLoadContext("test-collectible-compiler", isCollectible: true);
		try
		{
			var compilerAssembly = compilerContext.LoadFromAssemblyPath(typeof(Compilation).Assembly.Location);
			var payload = CopyTo(root, typeof(Given_CollectibleAnalyzerAssemblyLoader).Assembly.Location);
			var loader = new CollectibleAnalyzerAssemblyLoader(compilerContext);

			var analyzerContext = AssemblyLoadContext.GetLoadContext(loader.LoadFromPath(payload));

			var resolved = analyzerContext!.LoadFromAssemblyName(new AssemblyName("Microsoft.CodeAnalysis"));
			Assert.AreSame(compilerAssembly, resolved, "compiler assemblies must unify to the compiler context's instance");
		}
		finally
		{
			compilerContext.Unload();
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	[Description(
		"The snapshot transform must swap every AnalyzerFileReference onto the collectible " +
		"loader while preserving FullPath (identity for diagnostics and equality), and the " +
		"swapped reference must actually force-load warning-free through the new loader.")]
	public void When_WithCollectibleAnalyzerReferences_Then_SwapsLoadersKeepingPaths()
	{
		var originalReference = new AnalyzerFileReference(typeof(Given_CollectibleAnalyzerAssemblyLoader).Assembly.Location, new PassthroughLoader());

		var lib = ProjectId.CreateNewId();
		using var workspace = new AdhocWorkspace();
		var solution = workspace.CurrentSolution
			.AddProject(lib, "Lib1", "Lib1", LanguageNames.CSharp)
			.AddAnalyzerReference(lib, originalReference)
			.WithCollectibleAnalyzerReferences();

		var swapped = solution.GetProject(lib)!.AnalyzerReferences.OfType<AnalyzerFileReference>().Single();
		Assert.AreEqual(originalReference.FullPath, swapped.FullPath, "the analyzer path must be preserved");
		Assert.AreNotEqual(originalReference, swapped, "the reference must carry the new loader (equality is path+loader)");

		// The swapped reference must actually load through the new loader: force materialization
		// and assert nothing gets reported (the load succeeds, in a collectible context).
		var reporter = new RecordingReporter();
		EmbeddedRoslyn.WarnOnAnalyzerLoadFailures(solution, reporter);
		Assert.AreEqual(0, reporter.Warnings.Count, string.Join(Environment.NewLine, reporter.Warnings));

		var loaded = swapped.GetAssembly();
		Assert.IsTrue(AssemblyLoadContext.GetLoadContext(loaded)!.IsCollectible, "the swapped loader must load collectible");
	}

	private static string CopyTo(string directory, string sourcePath, string? fileName = null)
	{
		var target = Path.Combine(directory, fileName ?? Path.GetFileName(sourcePath));
		File.Copy(sourcePath, target);
		return target;
	}

	private sealed class PassthroughLoader : IAnalyzerAssemblyLoader
	{
		public void AddDependencyLocation(string fullPath)
		{
		}

		public Assembly LoadFromPath(string fullPath)
			=> Assembly.LoadFrom(fullPath);
	}
}
