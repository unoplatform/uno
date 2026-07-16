using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.HotReload.Tests.TestUtils;
using Uno.HotReload.Utils;

namespace Uno.HotReload.Tests.Utils;

/// <summary>
/// Tests for <see cref="RoslynExtensions.AlignHeadProjectCompilationOutputs"/> — the pass
/// re-pointing the kept head flavor's compilation outputs to the assembly the running
/// application was actually built from (RID-specific build outputs).
/// </summary>
[TestClass]
public sealed class Given_AlignHeadProjectCompilationOutputs
{
	private static readonly string _headPath = Given_TryGetTargetFramework.ToOsPath("/src/app/app.csproj");

	public TestContext TestContext { get; set; } = null!;

	private string _outputRoot = null!;

	[TestInitialize]
	public void Initialize()
		=> _outputRoot = Directory.CreateTempSubdirectory("uno_hr_align_").FullName;

	[TestCleanup]
	public void Cleanup()
	{
		try
		{
			Directory.Delete(_outputRoot, recursive: true);
		}
		catch (IOException)
		{
			// Never fail a test on temp cleanup.
		}
	}

	[TestMethod]
	public void When_NoRid_And_OutputReadable_Then_SolutionUnchanged()
	{
		var evaluatedPath = EmitAssembly(Path.Combine(_outputRoot, "app.dll"));
		using var workspace = new AdhocWorkspace();
		AddProject(workspace, "app(net10.0-desktop)", _headPath, evaluatedPath);

		var reporter = new RecordingReporter();
		var solution = workspace.CurrentSolution;
		var aligned = solution.AlignHeadProjectCompilationOutputs(_headPath, runtimeIdentifier: null, reporter);

		Assert.AreSame(solution, aligned);
		Assert.AreEqual(0, reporter.Warnings.Count);
	}

	[TestMethod]
	public void When_NoRid_And_OutputMissing_Then_Warns()
	{
		var evaluatedPath = Path.Combine(_outputRoot, "app.dll"); // never emitted
		using var workspace = new AdhocWorkspace();
		AddProject(workspace, "app(net10.0-desktop)", _headPath, evaluatedPath);

		var reporter = new RecordingReporter();
		var aligned = workspace.CurrentSolution.AlignHeadProjectCompilationOutputs(_headPath, runtimeIdentifier: null, reporter);

		Assert.AreSame(workspace.CurrentSolution, aligned);
		StringAssert.Contains(reporter.Warnings.Single(), evaluatedPath);
	}

	[TestMethod]
	public void When_Rid_Then_RidSpecificOutputPreferred()
	{
		// Both the RID-less twin and the RID-specific output exist: the RID-specific one is the
		// binary that was deployed, the RID-less one can be stale — it must lose.
		var evaluatedPath = EmitAssembly(Path.Combine(_outputRoot, "app.dll"));
		var ridPath = EmitAssembly(Path.Combine(_outputRoot, "android-x64", "app.dll"));
		using var workspace = new AdhocWorkspace();
		var projectId = AddProject(workspace, "app(net10.0-android)", _headPath, evaluatedPath);

		var reporter = new RecordingReporter();
		var aligned = workspace.CurrentSolution.AlignHeadProjectCompilationOutputs(_headPath, "android-x64", reporter);

		Assert.AreEqual(ridPath, aligned.GetProject(projectId)!.CompilationOutputInfo.AssemblyPath);
		Assert.AreEqual(0, reporter.Warnings.Count);
	}

	[TestMethod]
	public void When_Rid_And_OnlyProbedCandidate_Then_Remapped_IgnoringReferenceAssemblies()
	{
		// No RID-named folder (custom output layout); the real output sits deeper in the
		// evaluated directory subtree. Reference-assembly folders must never be picked, even
		// when their files are more recent.
		var evaluatedPath = Path.Combine(_outputRoot, "app.dll"); // never emitted
		var candidatePath = EmitAssembly(Path.Combine(_outputRoot, "custom", "app.dll"));
		var refPath = EmitAssembly(Path.Combine(_outputRoot, "ref", "app.dll"));
		var refintPath = EmitAssembly(Path.Combine(_outputRoot, "refint", "app.dll"));
		File.SetLastWriteTimeUtc(refPath, DateTime.UtcNow.AddHours(1));
		File.SetLastWriteTimeUtc(refintPath, DateTime.UtcNow.AddHours(1));

		using var workspace = new AdhocWorkspace();
		var projectId = AddProject(workspace, "app(net10.0-android)", _headPath, evaluatedPath);

		var reporter = new RecordingReporter();
		var aligned = workspace.CurrentSolution.AlignHeadProjectCompilationOutputs(_headPath, "android-x64", reporter);

		Assert.AreEqual(candidatePath, aligned.GetProject(projectId)!.CompilationOutputInfo.AssemblyPath);
	}

	[TestMethod]
	public void When_Rid_And_OnlyRidlessOutput_Then_EvaluatedKept()
	{
		var evaluatedPath = EmitAssembly(Path.Combine(_outputRoot, "app.dll"));
		using var workspace = new AdhocWorkspace();
		var projectId = AddProject(workspace, "app(net10.0-android)", _headPath, evaluatedPath);

		var reporter = new RecordingReporter();
		var aligned = workspace.CurrentSolution.AlignHeadProjectCompilationOutputs(_headPath, "android-x64", reporter);

		Assert.AreEqual(evaluatedPath, aligned.GetProject(projectId)!.CompilationOutputInfo.AssemblyPath);
		Assert.AreEqual(0, reporter.Warnings.Count);
	}

	[TestMethod]
	public void When_Rid_And_NothingReadable_Then_Warns_And_OtherProjectsUntouched()
	{
		// The RID path exists but is not a PE — module readability is the acceptance gate, mere
		// file existence is not enough. A non-head project with a dead output path must not
		// produce any warning: only the head flavor is aligned.
		var evaluatedPath = Path.Combine(_outputRoot, "app.dll"); // never emitted
		Directory.CreateDirectory(Path.Combine(_outputRoot, "android-x64"));
		File.WriteAllText(Path.Combine(_outputRoot, "android-x64", "app.dll"), "not a PE");

		using var workspace = new AdhocWorkspace();
		AddProject(workspace, "app(net10.0-android)", _headPath, evaluatedPath);
		AddProject(workspace, "lib", Given_TryGetTargetFramework.ToOsPath("/src/lib/lib.csproj"), Path.Combine(_outputRoot, "lib", "lib.dll"));

		var reporter = new RecordingReporter();
		var aligned = workspace.CurrentSolution.AlignHeadProjectCompilationOutputs(_headPath, "android-x64", reporter);

		Assert.AreSame(workspace.CurrentSolution, aligned);
		StringAssert.Contains(reporter.Warnings.Single(), "app.dll");
	}

	[TestMethod]
	public void When_OutputPathUnset_Then_Warns()
	{
		using var workspace = new AdhocWorkspace();
		AddProject(workspace, "app(net10.0-android)", _headPath, assemblyPath: null);

		var reporter = new RecordingReporter();
		var aligned = workspace.CurrentSolution.AlignHeadProjectCompilationOutputs(_headPath, "android-x64", reporter);

		Assert.AreSame(workspace.CurrentSolution, aligned);
		Assert.AreEqual(1, reporter.Warnings.Count);
	}

	// ────────────────────────────────────────────────────────────────────────
	// Fixture
	// ────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Emits a minimal valid assembly at <paramref name="path"/> — the alignment accepts a
	/// candidate only when its module (MVID) is readable, so the fixture needs real PE files.
	/// </summary>
	private static string EmitAssembly(string path)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);

		var compilation = CSharpCompilation.Create(
			Path.GetFileNameWithoutExtension(path),
			references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var emit = compilation.Emit(path);
		Assert.IsTrue(emit.Success, $"Fixture assembly emit failed: {string.Join(", ", emit.Diagnostics)}");
		return path;
	}

	private static ProjectId AddProject(AdhocWorkspace workspace, string name, string filePath, string? assemblyPath)
	{
		var projectId = ProjectId.CreateNewId(name);

		var projectInfo = ProjectInfo.Create(
			projectId,
			VersionStamp.Default,
			name: name,
			assemblyName: name,
			language: LanguageNames.CSharp,
			filePath: filePath)
			.WithCompilationOutputInfo(default(CompilationOutputInfo).WithAssemblyPath(assemblyPath));

		workspace.AddProject(projectInfo);
		return projectId;
	}
}
