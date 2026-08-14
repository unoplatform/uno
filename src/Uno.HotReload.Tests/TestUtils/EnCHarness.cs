using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Uno.HotReload.Microsoft;
using Uno.HotReload.Utils;

namespace Uno.HotReload.Tests.TestUtils;

/// <summary>
/// Minimal real-EnC session: a single-project solution whose baseline module is genuinely
/// compiled to disk (embedded PDB, exactly like the manager's <c>EmitCompilationOutputAsync</c>
/// bootstrap) so <see cref="WatchHotReloadService"/> runs the actual Roslyn EnC pipeline, not a
/// stub. The baseline references ConflictLib v1 and uses it from the single document.
/// </summary>
internal sealed class EnCHarness : IDisposable
{
	public const string ConflictLibSource = """
		namespace Conflict;
		public static class Info
		{
			public static string Version => "1";
		}
		""";

	// The exact capability set reported by the .NET 10 CoreCLR (MetadataUpdater.GetCapabilities()).
	private static readonly string[] _capabilities =
		"Baseline AddMethodToExistingType AddStaticFieldToExistingType AddInstanceFieldToExistingType NewTypeDefinition ChangeCustomAttributes UpdateParameters GenericUpdateMethod GenericAddMethodToExistingType GenericAddFieldToExistingType"
			.Split(' ');

	public required AdhocWorkspace Workspace { get; init; }
	public required WatchHotReloadService Watch { get; init; }
	public required Solution Solution { get; init; }
	public required ProjectId ProjectId { get; init; }
	public required DocumentId DocumentId { get; init; }

	/// <summary>
	/// Whether disposing the harness ends the EnC session. Defaults to <see langword="true"/>
	/// (shim-level tests drive <see cref="Watch"/> directly); tests that hand <see cref="Watch"/>
	/// to a <see cref="HotReloadManager"/> must set this to <see langword="false"/> — the
	/// manager's <c>Dispose</c> ends the session, and ending it twice throws.
	/// </summary>
	public bool OwnsSession { get; set; } = true;

	public static string AppSource(string expression)
		=> $$"""
		public class C
		{
			public static string M() => {{expression}};
		}
		""";

	public static SourceText AppText(string expression)
		=> SourceText.From(AppSource(expression), Encoding.UTF8);

	/// <summary>
	/// Wraps a complete document body, for the tests that need a baseline shape
	/// <see cref="AppSource"/> cannot express (a base type, several types, …).
	/// </summary>
	public static SourceText RawText(string source)
		=> SourceText.From(source, Encoding.UTF8);

	/// <param name="appSource">
	/// The baseline content of the single document. Defaults to the ConflictLib-consuming shape used
	/// by the reference-identity tests.
	/// </param>
	public static async Task<EnCHarness> CreateAsync(TempDirectory temp, CancellationToken ct, string? appSource = null)
	{
		var v1 = EmitLibrary(temp, "ConflictLib", "1.0.0.0", ConflictLibSource, subdir: "v1");

		var projectId = ProjectId.CreateNewId();
		var documentId = DocumentId.CreateNewId(projectId);
		var appPath = Path.Join(temp.Path, "App.cs");
		var appDll = Path.Join(temp.Path, "bin", "App.dll");

		var projectInfo = ProjectInfo.Create(
				projectId,
				VersionStamp.Create(),
				name: "App",
				assemblyName: "App",
				language: LanguageNames.CSharp,
				filePath: Path.Join(temp.Path, "App.csproj"),
				compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
				documents:
				[
					DocumentInfo.Create(
						documentId,
						name: "App.cs",
						loader: TextLoader.From(TextAndVersion.Create(
							appSource is null ? AppText("\"v\" + Conflict.Info.Version") : RawText(appSource),
							VersionStamp.Create(),
							appPath)),
						filePath: appPath),
				],
				metadataReferences:
				[
					MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
					MetadataReference.CreateFromFile(v1),
				])
			.WithCompilationOutputInfo(default(CompilationOutputInfo).WithAssemblyPath(appDll));

		var workspace = new AdhocWorkspace();
		workspace.AddProject(projectInfo);
		var solution = workspace.CurrentSolution;

		var emitted = await solution.EmitCompilationOutputAsync(ct);
		emitted.EnsureSuccess();

		var watch = await WatchHotReloadService.CreateAsync(solution, _capabilities, ct);

		return new EnCHarness
		{
			Workspace = workspace,
			Watch = watch,
			Solution = solution,
			ProjectId = projectId,
			DocumentId = documentId,
		};
	}

	public static string EmitLibrary(TempDirectory temp, string name, string version, string source, string? subdir = null)
	{
		var compilation = CSharpCompilation.Create(
			name,
			[
				CSharpSyntaxTree.ParseText(source, cancellationToken: CancellationToken.None),
				CSharpSyntaxTree.ParseText($"[assembly: System.Reflection.AssemblyVersion(\"{version}\")]", cancellationToken: CancellationToken.None),
			],
			[MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var dir = subdir is null ? temp.Path : Path.Join(temp.Path, subdir);
		Directory.CreateDirectory(dir);
		var path = Path.Join(dir, $"{name}.dll");
		var emit = compilation.Emit(path);
		if (!emit.Success)
		{
			throw new InvalidOperationException($"Failed to emit {name}: {string.Join(", ", emit.Diagnostics)}");
		}

		return path;
	}

	public void Dispose()
	{
		if (OwnsSession)
		{
			Watch.EndSession();
		}

		Workspace.Dispose();
	}
}
