using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
			Assert.AreSame(assembly1, loader.LoadFromPath(payload1), "same path must keep yielding the same assembly instance");
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

	[TestMethod]
	[Description(
		"Resolution must be DELEGATED to the compiler's context (LoadFromAssemblyName) so its " +
		"load policy can materialize dependencies it has not loaded YET: probing only its " +
		"already-loaded assemblies would fall through and load a split-identity local copy.")]
	public void When_CompilerContextResolvesLazily_Then_AnalyzerRequestsUnifyToIt()
	{
		var root = Directory.CreateTempSubdirectory("uno-hr-tests").FullName;
		var csharpPath = typeof(CSharpCompilation).Assembly.Location;
		var compilerContext = new OnDemandContext(new Dictionary<string, string> { ["Microsoft.CodeAnalysis.CSharp"] = csharpPath });
		try
		{
			var payload = CopyTo(root, typeof(Given_CollectibleAnalyzerAssemblyLoader).Assembly.Location);
			CopyTo(root, csharpPath); // A same-directory copy that must NOT win over the compiler's.
			var loader = new CollectibleAnalyzerAssemblyLoader(compilerContext);
			var analyzerContext = AssemblyLoadContext.GetLoadContext(loader.LoadFromPath(payload))!;

			Assert.IsFalse(compilerContext.Assemblies.Any(), "premise: the compiler context resolves lazily, nothing loaded yet");

			var resolved = analyzerContext.LoadFromAssemblyName(new AssemblyName("Microsoft.CodeAnalysis.CSharp"));

			Assert.AreSame(compilerContext, AssemblyLoadContext.GetLoadContext(resolved),
				"the request must materialize the dependency IN the compiler's context, not load the analyzer-local copy");
		}
		finally
		{
			compilerContext.Unload();
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	[Description(
		"AddDependencyLocation must retain EVERY location registered for a file name (distinct " +
		"projects may use distinct versions of the same analyzer package): resolution picks the " +
		"version matching the request, not whichever project registered that file name first.")]
	public void When_MultipleVersionsRegistered_Then_RequestedVersionWins()
	{
		var root = Directory.CreateTempSubdirectory("uno-hr-tests").FullName;
		try
		{
			var v1 = EmitAssembly(Path.Join(root, "v1", "Dep.dll"), "Dep", new Version(1, 0, 0, 0));
			var v2 = EmitAssembly(Path.Join(root, "v2", "Dep.dll"), "Dep", new Version(2, 0, 0, 0));
			var payload = CopyTo(Directory.CreateDirectory(Path.Join(root, "analyzer")).FullName, typeof(Given_CollectibleAnalyzerAssemblyLoader).Assembly.Location);

			var loader = new CollectibleAnalyzerAssemblyLoader();
			loader.AddDependencyLocation(v1); // Registered FIRST: "first file name wins" would pick this one.
			loader.AddDependencyLocation(v2);
			var analyzerContext = AssemblyLoadContext.GetLoadContext(loader.LoadFromPath(payload))!;

			var resolved = analyzerContext.LoadFromAssemblyName(new AssemblyName("Dep, Version=2.0.0.0"));

			Assert.AreEqual(new Version(2, 0, 0, 0), resolved.GetName().Version, "the exact requested version must win over the first registered one");
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	[Description(
		"When the exact requested version is not registered, resolution must fall back to the " +
		"highest registered version with a compatible identity — a fresh context is required " +
		"here: once a context has loaded a simple name, its own by-name cache answers (or " +
		"rejects) later requests without ever calling Load() again.")]
	public void When_RequestedVersionNotRegistered_Then_HighestCompatibleWins()
	{
		var root = Directory.CreateTempSubdirectory("uno-hr-tests").FullName;
		try
		{
			var v1 = EmitAssembly(Path.Join(root, "v1", "Dep.dll"), "Dep", new Version(1, 0, 0, 0));
			var v2 = EmitAssembly(Path.Join(root, "v2", "Dep.dll"), "Dep", new Version(2, 0, 0, 0));
			var payload = CopyTo(Directory.CreateDirectory(Path.Join(root, "analyzer")).FullName, typeof(Given_CollectibleAnalyzerAssemblyLoader).Assembly.Location);

			var loader = new CollectibleAnalyzerAssemblyLoader();
			loader.AddDependencyLocation(v1);
			loader.AddDependencyLocation(v2);
			var analyzerContext = AssemblyLoadContext.GetLoadContext(loader.LoadFromPath(payload))!;

			// 1.5 is not registered: the scan must pick the highest compatible version (2.0) —
			// which also satisfies the runtime's own version validation on the returned assembly.
			var resolved = analyzerContext.LoadFromAssemblyName(new AssemblyName("Dep, Version=1.5.0.0"));

			Assert.AreEqual(new Version(2, 0, 0, 0), resolved.GetName().Version, "the highest compatible registered version must win");
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	[Description(
		"A registered candidate whose identity does not match the request (public key token) " +
		"must be skipped — not force-loaded because its file name matches — leaving the " +
		"request to the runtime's own resolution.")]
	public void When_RequestedTokenDoesNotMatch_Then_CandidatesAreSkipped()
	{
		var root = Directory.CreateTempSubdirectory("uno-hr-tests").FullName;
		try
		{
			var v1 = EmitAssembly(Path.Join(root, "v1", "Dep.dll"), "Dep", new Version(1, 0, 0, 0));
			var v2 = EmitAssembly(Path.Join(root, "v2", "Dep.dll"), "Dep", new Version(2, 0, 0, 0));
			var payload = CopyTo(Directory.CreateDirectory(Path.Join(root, "analyzer")).FullName, typeof(Given_CollectibleAnalyzerAssemblyLoader).Assembly.Location);

			var loader = new CollectibleAnalyzerAssemblyLoader();
			loader.AddDependencyLocation(v1);
			loader.AddDependencyLocation(v2);
			var analyzerContext = AssemblyLoadContext.GetLoadContext(loader.LoadFromPath(payload))!;

			// Both candidates are unsigned: a request carrying a public key token must skip them
			// (and then fail in the runtime's own resolution, since nobody can provide it).
			Assert.ThrowsExactly<FileNotFoundException>(() =>
				analyzerContext.LoadFromAssemblyName(new AssemblyName("Dep, Version=2.0.0.0, PublicKeyToken=b03f5f7f11d50a3a")));
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	[Description(
		"A shadow directory holding partial state — an orphaned staging file and a pdb " +
		"published without its dll, i.e. a process killed mid-publish — must not prevent the " +
		"load: the dll's atomic publish is what completes the bundle.")]
	public void When_ShadowDirectoryHoldsPartialState_Then_LoadRecovers()
	{
		var root = Directory.CreateTempSubdirectory("uno-hr-tests").FullName;
		try
		{
			var payload = CopyTo(root, typeof(Given_CollectibleAnalyzerAssemblyLoader).Assembly.Location);

			// Re-create the debris a kill between the pdb and dll publishes would leave behind.
			var shadowPath = CollectibleAnalyzerAssemblyLoader.GetShadowCopyPath(payload);
			Assert.IsFalse(File.Exists(shadowPath), "premise: this (path, timestamp, size) key must be fresh");
			Directory.CreateDirectory(Path.GetDirectoryName(shadowPath)!);
			File.WriteAllText($"{shadowPath}.{Guid.NewGuid():N}.staging", "debris from a killed copy");
			File.WriteAllBytes(Path.ChangeExtension(shadowPath, ".pdb"), [1, 2, 3]);

			var loaded = new CollectibleAnalyzerAssemblyLoader().LoadFromPath(payload);

			Assert.IsNotNull(loaded);
			Assert.IsTrue(File.Exists(shadowPath), "the dll must have been published despite the pre-existing debris");
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	private static string CopyTo(string directory, string sourcePath, string? fileName = null)
	{
		var target = Path.Join(directory, fileName ?? Path.GetFileName(sourcePath));
		File.Copy(sourcePath, target);
		return target;
	}

	/// <summary>
	/// Emits a minimal assembly with the requested identity — the best-candidate resolution
	/// reads the real <see cref="AssemblyName"/> from disk, so the fixture needs real PE files.
	/// </summary>
	private static string EmitAssembly(string path, string name, Version version)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);

		var compilation = CSharpCompilation.Create(
			name,
			[CSharpSyntaxTree.ParseText($"""[assembly: System.Reflection.AssemblyVersion("{version}")]""")],
			references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var emit = compilation.Emit(path);
		Assert.IsTrue(emit.Success, $"Fixture assembly emit failed: {string.Join(", ", emit.Diagnostics)}");
		return path;
	}

	/// <summary>
	/// A compiler-side context that — like the dev-server's per-application context — can
	/// RESOLVE assemblies it has not loaded yet (lazy, on demand).
	/// </summary>
	private sealed class OnDemandContext(IReadOnlyDictionary<string, string> resolvable)
		: AssemblyLoadContext("test-on-demand-compiler", isCollectible: true)
	{
		protected override Assembly? Load(AssemblyName assemblyName)
			=> assemblyName.Name is { } name && resolvable.TryGetValue(name, out var path)
				? LoadFromAssemblyPath(path)
				: null;
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
