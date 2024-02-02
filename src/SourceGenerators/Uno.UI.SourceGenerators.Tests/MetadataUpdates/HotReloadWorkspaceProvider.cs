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
using Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates;
using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.MetadataUpdates;

internal class HotReloadWorkspace
{
	public record UpdateResult(ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<WatchHotReloadService.Update> MetadataUpdates);

	const string NetCoreCapsRaw = "Baseline AddMethodToExistingType AddStaticFieldToExistingType AddInstanceFieldToExistingType NewTypeDefinition ChangeCustomAttributes UpdateParameters";
	const string MonoCapsRaw = "Baseline AddMethodToExistingType AddStaticFieldToExistingType NewTypeDefinition ChangeCustomAttributes";

	private readonly string _baseWorkFolder;
	private readonly bool _isDebugCompilation;
	private readonly bool _isMono;
	private readonly bool _useXamlReaderReload;

	private Dictionary<string, string[]> _projects = new();
	private Dictionary<string, Dictionary<string, string>> _sourceFiles = new();
	private Dictionary<string, Dictionary<string, string>> _additionalFiles = new();

	private Solution? _currentSolution;
	private WatchHotReloadService? _hotReloadService;

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

	public void SetSourceFile(string project, string fileName, string content)
	{
		if (_sourceFiles.TryGetValue(project, out var files))
		{
			files[fileName] = content;
		}
		else
		{
			_sourceFiles[project] = new()
			{
				[fileName] = content
			};
		}
		var basePath = Path.Combine(_baseWorkFolder, project);
		var filePath = Path.Combine(basePath, fileName);
		Directory.CreateDirectory(basePath);
		File.WriteAllText(filePath, content, Encoding.UTF8);

		if (_currentSolution is not null)
		{
			var documents = _currentSolution
				.Projects
				.SelectMany(p => p.Documents)
				.First(d => d.FilePath == filePath);

			_currentSolution = _currentSolution.WithDocumentText(
				documents.Id,
				CSharpSyntaxTree.ParseText(content, encoding: Encoding.UTF8).GetText());
		}
	}

	public void SetAdditionalFile(string project, string fileName, string content)
	{
		if (_additionalFiles.TryGetValue(project, out var files))
		{
			files[fileName] = content;
		}
		else
		{
			_additionalFiles[project] = new()
			{
				[fileName] = content
			};
		}

		var basePath = Path.Combine(_baseWorkFolder, project);
		Directory.CreateDirectory(basePath);
		var filePath = Path.Combine(basePath, fileName);
		File.WriteAllText(filePath, content, Encoding.UTF8);

		if (_currentSolution is not null)
		{
			var documents = _currentSolution
				.Projects
				.SelectMany(p => p.AdditionalDocuments)
				.First(d => d.FilePath == filePath);

			_currentSolution = _currentSolution.WithAdditionalDocumentText(
				documents.Id,
				CSharpSyntaxTree.ParseText(content, encoding: Encoding.UTF8).GetText());
		}
	}

	public async Task Initialize(CancellationToken ct)
	{
		TaskCompletionSource<bool> taskCompletionSource = new();

		var workspace = new AdhocWorkspace();

		workspace.WorkspaceFailed += (_sender, diag) =>
		{
			if (diag.Diagnostic.Kind == WorkspaceDiagnosticKind.Warning)
			{
				Console.WriteLine($"MSBuildWorkspace warning: {diag.Diagnostic}");
			}
			else
			{
				taskCompletionSource.TrySetException(new InvalidOperationException($"Failed to create MSBuildWorkspace: {diag.Diagnostic}"));
			}
		};

		var currentSolution = workspace.CurrentSolution;
		var frameworkReferences = BuildFrameworkReferences();
		var references = BuildUnoReferences().Concat(frameworkReferences);

		var generatorReference = new MyGeneratorReference(
			ImmutableArray.Create<ISourceGenerator>(new XamlGenerator.XamlCodeGenerator()));

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
								assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default),
							analyzerReferences: new[] { generatorReference });

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

				if (additionalFiles.Any())
				{
					// Build the analyzer document additional data information
					var analyzerDocumentId = DocumentId.CreateNewId(project.Id);

					// For now, there is no need to customize these for each test.
					var globalConfigBuilder = new StringBuilder($"""
						is_global = true
						build_property.MSBuildProjectFullPath = C:\Project\{project.Name}.csproj
						build_property.RootNamespace = {project.Name}
						build_property.XamlSourceGeneratorTracingFolder = {_baseWorkFolder}
						build_property.Configuration = {(_isDebugCompilation ? "Debug" : "Release")}
						
						"""); ;

					foreach (var (fileName, content) in additionalFiles.Where(k => k.Key.EndsWith(".xaml")))
					{
						globalConfigBuilder.Append($"""
							[{Path.Combine(_baseWorkFolder, project.Name, fileName).Replace("\\", "/")}]
							build_metadata.AdditionalFiles.SourceItemGroup = Page
							""");
					}

					currentSolution = currentSolution.AddAnalyzerConfigDocument(
						analyzerDocumentId,
						name: ".globalconfig",
						filePath: "/.globalconfig",
						text: SourceText.From(globalConfigBuilder.ToString())
					); ;
				}
			}

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

			var emitResult = c.Emit(
				p.CompilationOutputInfo.AssemblyPath!,
				pdbPath: Path.ChangeExtension(p.CompilationOutputInfo.AssemblyPath, ".pdb"));

			if (!emitResult.Success)
			{
				throw new InvalidOperationException($"Emit errors: {string.Join("\n", emitResult.Diagnostics)}");
			}
		}

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

		var (updates, diagnostics) = await _hotReloadService.EmitSolutionUpdateAsync(_currentSolution, CancellationToken.None);

		return new(diagnostics, updates);
	}

	private static PortableExecutableReference[] BuildFrameworkReferences()
		=> Directory.GetFiles(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System*.dll")
				.Where(f => !f.Contains(".Native", StringComparison.OrdinalIgnoreCase))
				.Select(f => MetadataReference.CreateFromFile(f))
				.ToArray();

	private static PortableExecutableReference[] BuildUnoReferences()
	{
		const string configuration =
#if DEBUG
			"Debug";
#else
			"Release";
#endif

		var availableTargets = new[] {
			Path.Combine("Uno.UI.Skia", configuration, "net7.0"),
			Path.Combine("Uno.UI.Reference", configuration, "net7.0"),
		};

		var unoUIBase = Path.Combine(
			Path.GetDirectoryName(typeof(HotReloadWorkspace).Assembly.Location)!,
			"..",
			"..",
			"..",
			"..",
			"..",
			"Uno.UI",
			"bin"
			);

		var unoTarget = availableTargets
			.Select(t => Path.Combine(unoUIBase, t, "Uno.UI.dll"))
			.FirstOrDefault(File.Exists);

		if (unoTarget is null)
		{
			throw new InvalidOperationException($"Unable to find Uno.UI.dll in {string.Join(',', availableTargets)}");
		}

		return Directory.GetFiles(Path.GetDirectoryName(unoTarget)!, "*.dll")
					.Select(f => MetadataReference.CreateFromFile(f))
					.ToArray();
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
