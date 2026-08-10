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

	public static string AppSource(string expression)
		=> $$"""
		public class C
		{
			public static string M() => {{expression}};
		}
		""";

	public static SourceText AppText(string expression)
		=> SourceText.From(AppSource(expression), Encoding.UTF8);

	public static async Task<EnCHarness> CreateAsync(TempDirectory temp, CancellationToken ct)
	{
		var v1 = EmitLibrary(temp, "ConflictLib", "1.0.0.0", ConflictLibSource, subdir: "v1");

		var projectId = ProjectId.CreateNewId();
		var documentId = DocumentId.CreateNewId(projectId);
		var appPath = Path.Combine(temp.Path, "App.cs");
		var appDll = Path.Combine(temp.Path, "bin", "App.dll");

		var projectInfo = ProjectInfo.Create(
				projectId,
				VersionStamp.Create(),
				name: "App",
				assemblyName: "App",
				language: LanguageNames.CSharp,
				filePath: Path.Combine(temp.Path, "App.csproj"),
				compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
				documents:
				[
					DocumentInfo.Create(
						documentId,
						name: "App.cs",
						loader: TextLoader.From(TextAndVersion.Create(
							AppText("\"v\" + Conflict.Info.Version"),
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

		var dir = subdir is null ? temp.Path : Path.Combine(temp.Path, subdir);
		Directory.CreateDirectory(dir);
		var path = Path.Combine(dir, $"{name}.dll");
		var emit = compilation.Emit(path);
		if (!emit.Success)
		{
			throw new InvalidOperationException($"Failed to emit {name}: {string.Join(", ", emit.Diagnostics)}");
		}

		return path;
	}

	public void Dispose()
		=> Workspace.Dispose();
}
