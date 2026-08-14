using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using Uno.HotReload;
using Uno.HotReload.Microsoft;
using Uno.HotReload.Utils;
using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.MetadataUpdates;

internal class HotReloadWorkspace : IDisposable
{
	public record UpdateResult(ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<Update> MetadataUpdates);

#if NET9_0
	// https://github.com/dotnet/runtime/blob/e99557baffbe864d624cc1c95c9cbf2eefae684f/src/coreclr/System.Private.CoreLib/src/System/Reflection/Metadata/MetadataUpdater.cs#L58
	const string NetCoreCapsRaw = "Baseline AddMethodToExistingType AddStaticFieldToExistingType AddInstanceFieldToExistingType NewTypeDefinition ChangeCustomAttributes UpdateParameters GenericUpdateMethod GenericAddMethodToExistingType GenericAddFieldToExistingType";
	// https://github.com/dotnet/runtime/blob/e99557baffbe864d624cc1c95c9cbf2eefae684f/src/mono/mono/component/hot_reload.c#L3330
	const string MonoCapsRaw = "Baseline AddMethodToExistingType AddStaticFieldToExistingType NewTypeDefinition ChangeCustomAttributes AddInstanceFieldToExistingType GenericAddMethodToExistingType GenericUpdateMethod UpdateParameters GenericAddFieldToExistingType";
#elif NET10_0
	// https://github.com/dotnet/runtime/blob/2db28217c40088686fcc8ccc52df2da0391bb712/src/coreclr/System.Private.CoreLib/src/System/Reflection/Metadata/MetadataUpdater.cs#L58
	const string NetCoreCapsRaw = "Baseline AddMethodToExistingType AddStaticFieldToExistingType AddInstanceFieldToExistingType NewTypeDefinition ChangeCustomAttributes UpdateParameters GenericUpdateMethod GenericAddMethodToExistingType GenericAddFieldToExistingType AddFieldRva";
	// https://github.com/dotnet/runtime/blob/2db28217c40088686fcc8ccc52df2da0391bb712/src/mono/mono/component/hot_reload.c#L3349
	const string MonoCapsRaw = "Baseline AddMethodToExistingType AddStaticFieldToExistingType NewTypeDefinition ChangeCustomAttributes AddInstanceFieldToExistingType GenericAddMethodToExistingType GenericUpdateMethod UpdateParameters GenericAddFieldToExistingType";
#else
#error This runtime is not supported yet, find the caps in the .NET runtime's sources
#endif

	private readonly string _baseWorkFolder;
	private readonly bool _isDebugCompilation;
	private readonly bool _isMono;
	private readonly bool _useXamlReaderReload;

	private Dictionary<string, string[]> _projects = new();
	private Dictionary<string, Dictionary<string, string>> _sourceFiles = new();
	private Dictionary<string, Dictionary<string, string>> _additionalFiles = new();

	private Solution? _currentSolution;
	private WatchHotReloadService? _hotReloadService;
	private AdhocWorkspace? _workspace;

	public HotReloadWorkspace(bool isDebugCompilation, bool isMono, bool useXamlReaderReload)
	{
		_isDebugCompilation = isDebugCompilation;
		_isMono = isMono;
		_useXamlReaderReload = useXamlReaderReload;
		_baseWorkFolder = Path.Combine(Path.GetDirectoryName(typeof(HotReloadWorkspace).Assembly.Location)!, "work");
		Directory.CreateDirectory(_baseWorkFolder);

		Environment.SetEnvironmentVariable("Microsoft_CodeAnalysis_EditAndContinue_LogDir", _baseWorkFolder);
	}

	internal void AddProject(string name, string[] references)
	{
		var exists = _projects.TryGetValue(name, out var existingReferences);
		if (exists
			&& existingReferences is not null
			&& !references.SequenceEqual(existingReferences))
		{
			throw new InvalidOperationException($"Project {name} already exists with a different set of project references");
		}

		if (!exists)
		{
			_projects.Add(name, references);
		}
	}

	public void UpdateSourceFile(string projectName, string fileName, string? content)
	{
		var basePath = Path.Combine(_baseWorkFolder, projectName);
		var filePath = Path.Combine(basePath, fileName);

		if (_currentSolution is null)
		{
			if (content is null)
			{
				return;
			}

			Directory.CreateDirectory(basePath);
			for (var i = 2; i >= 0; i--)
			{
				try
				{
					File.WriteAllText(filePath, content, Encoding.UTF8);
					break;
				}
				catch (IOException) when (i is not 0)
				{
					Task.Delay(100).Wait();
				}
			}

			if (_sourceFiles.TryGetValue(projectName, out var files))
			{
				files[fileName] = content;
			}
			else
			{
				_sourceFiles[projectName] = new()
				{
					[fileName] = content
				};
			}
		}
		else
		{
			var project = _currentSolution.Projects.FirstOrDefault(p => p.Name == projectName);
			if (project is null)
			{
				throw new InvalidOperationException($"Project {projectName} not found in the current solution");
			}

			var doc = project.Documents.FirstOrDefault(d => d.FilePath == filePath);
			_currentSolution = (doc, content) switch
			{
				(null, not null) => _currentSolution.AddDocument(
					DocumentId.CreateNewId(project.Id),
					fileName,
					CSharpSyntaxTree.ParseText(content, encoding: Encoding.UTF8).GetText(),
					filePath: Path.Combine(_baseWorkFolder, projectName, fileName)
				),

				(not null, not null) => _currentSolution.WithDocumentText(doc.Id, CSharpSyntaxTree.ParseText(content, encoding: Encoding.UTF8).GetText()),

				(not null, null) => _currentSolution.RemoveDocument(doc.Id),

				_ => _currentSolution
			};
		}
	}

	public void UpdateAdditionalFile(string projectName, string fileName, string? content)
	{
		var basePath = Path.Combine(_baseWorkFolder, projectName);
		var filePath = Path.Combine(basePath, fileName);

		if (_currentSolution is null)
		{
			if (content is null)
			{
				return;
			}

			Directory.CreateDirectory(basePath);
			File.WriteAllText(filePath, content, Encoding.UTF8);

			if (_additionalFiles.TryGetValue(projectName, out var files))
			{
				files[fileName] = content;
			}
			else
			{
				_additionalFiles[projectName] = new()
				{
					[fileName] = content
				};
			}
		}
		else
		{
			var project = _currentSolution.Projects.FirstOrDefault(p => p.Name == projectName);
			if (project is null)
			{
				throw new InvalidOperationException($"Project {projectName} not found in the current solution");
			}

			var doc = project.AdditionalDocuments.FirstOrDefault(d => d.FilePath == filePath);
			_currentSolution = (doc, content) switch
			{
				(null, not null) => _currentSolution.AddAdditionalDocument(
					DocumentId.CreateNewId(project.Id),
					fileName,
					CSharpSyntaxTree.ParseText(content, encoding: Encoding.UTF8).GetText(),
					filePath: Path.Combine(_baseWorkFolder, projectName, fileName)
				),

				(not null, not null) => _currentSolution.WithAdditionalDocumentText(doc.Id, CSharpSyntaxTree.ParseText(content, encoding: Encoding.UTF8).GetText()),

				(not null, null) => _currentSolution.RemoveAdditionalDocument(doc.Id),

				_ => _currentSolution
			};
		}
	}

	public async Task Initialize(CancellationToken ct)
	{
		TaskCompletionSource<bool> taskCompletionSource = new();

		var workspace = _workspace = new AdhocWorkspace();

		void OnWorkspaceFailed(WorkspaceDiagnostic diagnostic)
		{
			if (diagnostic.Kind == WorkspaceDiagnosticKind.Warning)
			{
				Console.WriteLine($"MSBuildWorkspace warning: {diagnostic}");
			}
			else
			{
				taskCompletionSource.TrySetException(new InvalidOperationException($"Failed to create MSBuildWorkspace: {diagnostic}"));
			}
		}

#if NET10_0_OR_GREATER
		// Roslyn 5.x obsoleted the WorkspaceFailed event (handlers are no longer marshaled to a UI
		// thread — a non-event here); the returned registration lives as long as the workspace.
		// Same shape as CompilationWorkspaceProvider, which the dev-server host uses.
		_ = workspace.RegisterWorkspaceFailedHandler(args => OnWorkspaceFailed(args.Diagnostic));
#else
		workspace.WorkspaceFailed += (_sender, diag) => OnWorkspaceFailed(diag.Diagnostic);
#endif

		var currentSolution = workspace.CurrentSolution;
		var frameworkReferences = BuildFrameworkReferences();
		var unoReferences = UnoAssemblyHelper.LoadAssemblies();
		var references = Enumerable.Concat(unoReferences, frameworkReferences);

		var generatorReference = new MyGeneratorReference([new XamlGenerator.XamlCodeGenerator()]);

		FillProjectsFromFiles();

		foreach (var projectName in EnumerateProjects())
		{
			var projectInfo = ProjectInfo.Create(
				ProjectId.CreateNewId(),
				VersionStamp.Default,
				name: projectName,
				assemblyName: projectName,
				language: LanguageNames.CSharp,
				filePath: Path.Combine(_baseWorkFolder, projectName + ".csproj"),
				outputFilePath: _baseWorkFolder,
				metadataReferences: references,
				compilationOptions: new CSharpCompilationOptions(
						OutputKind.DynamicallyLinkedLibrary,
						optimizationLevel: OptimizationLevel.Debug,
						allowUnsafe: true,
						nullableContextOptions: NullableContextOptions.Enable,
						assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default)
					.WithSpecificDiagnosticOptions([new("CS1701", ReportDiagnostic.Suppress)]), // Assuming assembly reference 'System.ObjectModel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' used by 'Uno.UI' matches identity 'System.ObjectModel, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' of 'System.ObjectModel', you may need to supply runtime policy, expected
				analyzerReferences: [generatorReference]);

			projectInfo = projectInfo
				.WithCompilationOutputInfo(
					projectInfo.CompilationOutputInfo.WithAssemblyPath(Path.Combine(_baseWorkFolder, projectName + Guid.NewGuid() + ".dll")));

			if (!_projects.TryGetValue(projectName, out var projectReferences))
			{
				throw new InvalidOperationException($"Project {projectName} is not defined in the project list.");
			}

			var project = workspace
				.AddProject(projectInfo)
				.AddProjectReferences(currentSolution
					.Projects
					.Where(p => projectReferences.Contains(p.Name))
					.Select(p => new ProjectReference(p.Id)));

			currentSolution = project.Solution;

			// Build the analyzer document additional data information
			var analyzerDocumentId = DocumentId.CreateNewId(project.Id);

			// A host-native absolute path, NOT a hardcoded "C:\Project\…": the generator derives
			// _projectDirectory from this with Path.GetDirectoryName, which returns "" for a
			// drive-letter path on Unix — and an empty project directory then mangles every path
			// it feeds (Substring(_projectDirectory.Length + 1) and friends), so no page is emitted.
			var projectFullPath = Path.Combine(_baseWorkFolder, project.Name, $"{project.Name}.csproj");

			// For now, there is no need to customize these for each test.
			var globalConfigBuilder = new StringBuilder($"""
				is_global = true
				build_property.MSBuildProjectFullPath = {projectFullPath}
				build_property.RootNamespace = {project.Name}
				build_property.XamlSourceGeneratorTracingFolder = {_baseWorkFolder}
				build_property.Configuration = {(_isDebugCompilation ? "Debug" : "Release")}

				""");
			if (_sourceFiles.TryGetValue(projectName, out var sourceFiles))
			{
				foreach (var (fileName, content) in sourceFiles)
				{
					var documentId = DocumentId.CreateNewId(project.Id);

					currentSolution = currentSolution.AddDocument(
						documentId,
						fileName,
						CSharpSyntaxTree.ParseText(content, encoding: Encoding.UTF8).GetText(),
						filePath: Path.Combine(_baseWorkFolder, project.Name, fileName)
					);
				}
			}

			if (_additionalFiles.TryGetValue(projectName, out var additionalFiles))
			{
				foreach (var (fileName, content) in additionalFiles)
				{
					var documentId = DocumentId.CreateNewId(project.Id);
					currentSolution = currentSolution.AddAdditionalDocument(
						documentId,
						fileName,
						CSharpSyntaxTree.ParseText(content, encoding: Encoding.UTF8).GetText(),
						filePath: Path.Combine(_baseWorkFolder, project.Name, fileName));
				}

				foreach (var (fileName, content) in additionalFiles.Where(k => k.Key.EndsWith(".xaml")))
				{
					globalConfigBuilder.Append($"""
						[{Path.Combine(_baseWorkFolder, project.Name, fileName).Replace("\\", "/")}]
						build_metadata.AdditionalFiles.SourceItemGroup = Page
						""");
				}
			}

			currentSolution = currentSolution.AddAnalyzerConfigDocument(
				analyzerDocumentId,
				name: ".globalconfig",
				filePath: "/.globalconfig",
				text: SourceText.From(globalConfigBuilder.ToString())
			);

			workspace.TryApplyChanges(currentSolution);
		}


		// Materialize the first build
		foreach (var p in currentSolution.Projects)
		{
			var c = await p.GetCompilationAsync(ct);

			if (c is null)
			{
				throw new InvalidOperationException($"Failed to get the compilation for {p}");
			}

			if (c.GetDiagnostics().Any(d => d.DefaultSeverity == DiagnosticSeverity.Error))
			{
				var errors = c.GetDiagnostics().Where(d => d.DefaultSeverity == DiagnosticSeverity.Error);

				throw new InvalidOperationException($"Compilation errors: {string.Join("\n", errors)}");
			}
		}

		// Emit through the dev-server host's own helper rather than Compilation.Emit(pdbPath:):
		// it writes an EMBEDDED pdb, whereas a separate .pdb defaults to the Windows format and
		// needs the native DiaSymReader, which does not exist on Linux -- every project then fails
		// with CS0041 "Unexpected error writing debug information -- Value cannot be null
		// (Parameter 'type')". Using the helper also keeps the baseline this session starts from
		// byte-identical to the one HotReloadManager captures in production.
		(await currentSolution.EmitCompilationOutputAsync(ct)).EnsureSuccess();

		var metadataUpdateCaps = (_isMono ? MonoCapsRaw : NetCoreCapsRaw).Split(" ");
		var hotReloadService = new WatchHotReloadService(workspace.Services, metadataUpdateCaps);
		await hotReloadService.StartSessionAsync(currentSolution, ct);

		_currentSolution = currentSolution;
		_hotReloadService = hotReloadService;
	}

	private IEnumerable<string> EnumerateProjects()
	{
		Stack<string> currentProjects = new();
		HashSet<string> processed = new();

		foreach (var project in InnerEnumerate(_projects.Keys))
		{
			yield return project;
		}

		IEnumerable<string> InnerEnumerate(IEnumerable<string> projects)
		{
			foreach (var project in projects)
			{
				if (currentProjects.Contains(project))
				{
					throw new InvalidOperationException($"Circular dependency detected: {string.Join(" -> ", currentProjects)} -> {project}");
				}

				currentProjects.Push(project);

				if (_projects.TryGetValue(project, out var children))
				{
					foreach (var child in InnerEnumerate(children))
					{
						yield return child;
					}
				}

				if (!processed.Contains(project))
				{
					yield return project;

					processed.Add(project);
				}

				currentProjects.Pop();
			}
		}
	}

	private void FillProjectsFromFiles()
	{
		var projects = _additionalFiles
			.Keys
			.Concat(_sourceFiles.Keys)
			.Distinct()
			.Except(_projects.Keys);

		foreach (var project in projects)
		{
			AddProject(project, Array.Empty<string>());
		}
	}

	public async Task<UpdateResult> Update()
	{
		if (_hotReloadService is null || _currentSolution is null)
		{
			throw new InvalidOperationException($"Initialize must be called before Update");
		}

		var (updates, diagnostics, _) = await _hotReloadService.EmitSolutionUpdateAsync(_currentSolution, CancellationToken.None);

		return new(diagnostics, updates);
	}

	private static PortableExecutableReference[] BuildFrameworkReferences()
		=> Directory.GetFiles(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System*.dll")
				.Where(f => !f.Contains(".Native", StringComparison.OrdinalIgnoreCase))
				.Select(f => MetadataReference.CreateFromFile(f))
				.ToArray();

	/// <summary>
	/// Releases the EnC session and the workspace of this scenario. Without it every scenario leaks
	/// a full Roslyn workspace, its compilations and an open EnC session with its baselines: the
	/// suite runs ~250 scenarios, and the accumulation gets the test host OOM-killed partway through
	/// (it never completed a full run, so the results were silently partial).
	/// </summary>
	public void Dispose()
	{
		// EndSession throws when no session was started, and when called twice.
		if (Interlocked.Exchange(ref _hotReloadService, null) is { } hotReloadService)
		{
			hotReloadService.EndSession();
		}

		Interlocked.Exchange(ref _workspace, null)?.Dispose();
		_currentSolution = null;
	}
}

sealed class MyGeneratorReference : AnalyzerReference
{
	private readonly ImmutableArray<ISourceGenerator> _generators;

	internal MyGeneratorReference(ImmutableArray<ISourceGenerator> generators)
	{
		_generators = generators;
	}

	public override string FullPath
	{
		get => "";
	}

	public override object Id
	{
		get => nameof(MyGeneratorReference);
	}

	public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
	{
		return ImmutableArray<DiagnosticAnalyzer>.Empty;
	}

	public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
	{
		return ImmutableArray<DiagnosticAnalyzer>.Empty;
	}

	public override ImmutableArray<ISourceGenerator> GetGenerators(string language)
	{
		return _generators;
	}
}
