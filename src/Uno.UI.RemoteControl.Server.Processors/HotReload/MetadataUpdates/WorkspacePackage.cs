using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Uno.UI.RemoteControl.Helpers;

namespace Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates;

internal static class WorkspacePackage
{
	/// <summary>
	/// Creates a package file (zip) for the specified solution asynchronously.
	/// </summary>
	/// <param name="solution">The solution for which to create the package file. Cannot be null.</param>
	/// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
	/// <returns>
	/// A ValueTask that represents the asynchronous operation.
	/// The result contains the path of the created package file.
	/// </returns>
	public static ValueTask<string> Create(Solution solution, CancellationToken ct)
		=> Create(solution, null, true, ct);

	/// <summary>
	/// Creates a package file (zip) for the specified solution asynchronously.
	/// </summary>
	/// <param name="solution">The solution for which to create the package file. Cannot be null.</param>
	/// <param name="targetFile">The path of the package file.</param>
	/// <param name="overwrite">Indicate if the <paramref name="targetFile"/> should be overriden or not.</param>
	/// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
	/// <returns>
	/// A ValueTask that represents the asynchronous operation.
	/// The result contains the path of the created package file (<paramref name="targetFile"/> if specified; otherwise, a temporary file path)."/>
	/// </returns>
	public static async ValueTask<string> Create(Solution solution, string? targetFile, bool overwrite, CancellationToken ct)
	{
		if (targetFile is not null && File.Exists(targetFile))
		{
			if (overwrite)
			{
				File.Delete(targetFile);
			}
			else
			{
				throw new IOException($"The file '{targetFile}' already exists.'");
			}
		}

		targetFile ??= Path.GetTempFileName();
		var workingDir = Path.Combine(Path.GetTempPath(), $"uno-hr-workspace-{Guid.NewGuid():N}");
		Directory.CreateDirectory(workingDir);

		try
		{
			var data = new WorkspaceData(Solution: solution.GetData());
			var packager = new Packager(workingDir);
			var packagedData = packager.PackWorkspace(data);

			await using (var manifestFile = File.Create(Path.Combine(workingDir, "manifest.json")))
			{
				await System.Text.Json.JsonSerializer.SerializeAsync(manifestFile, packagedData, _jsonOptions, ct);
			}

			await ZipFile.CreateFromDirectoryAsync(workingDir, targetFile, CompressionLevel.Optimal, includeBaseDirectory: false, ct);

			return targetFile;
		}
		finally
		{
			try
			{
				Directory.Delete(workingDir, recursive: true);
			}
			catch
			{
				// Ignore cleanup failures.
			}
		}
	}

	/// <summary>
	/// Extracts a workspace from the specified package file and returns its manifest data.
	/// </summary>
	/// <remarks>
	/// If the specified working directory does not exist, it is created.
	/// If extraction fails and a new working directory was created, the directory is deleted as a best-effort cleanup.
	/// </remarks>
	/// <param name="packageFile">The path to the package file to extract. Must be a valid workspace archive file (zip) containing a manifest.json file.</param>
	/// <param name="ct">A cancellation token that can be used to cancel the extraction operation.</param>
	/// <returns>
	/// A task that represents the asynchronous extraction operation.
	/// The task result contains the workspace data deserialized from the manifest.json file.</returns>
	/// <exception cref="InvalidOperationException">Thrown if the package does not contain a manifest.json file or if the manifest cannot be read.</exception>
	public static ValueTask<WorkspaceData> Extract(string packageFile, CancellationToken ct)
		=> Extract(packageFile, null, true, ct);

	/// <summary>
	/// Extracts a workspace from the specified package file and returns its manifest data.
	/// </summary>
	/// <remarks>
	/// If the specified working directory does not exist, it is created.
	/// If extraction fails and a new working directory was created, the directory is deleted as a best-effort cleanup.
	/// </remarks>
	/// <param name="packageFile">The path to the package file to extract. Must be a valid workspace archive file (zip) containing a manifest.json file.</param>
	/// <param name="workingDir">The directory in which to extract the package contents. If null, a temporary directory is created and used.</param>
	/// <param name="overwrite">Indicate if the <paramref name="workingDir"/> should be cleaned or not.</param>
	/// <param name="ct">A cancellation token that can be used to cancel the extraction operation.</param>
	/// <returns>
	/// A task that represents the asynchronous extraction operation.
	/// The task result contains the workspace data deserialized from the manifest.json file.</returns>
	/// <exception cref="InvalidOperationException">Thrown if the package does not contain a manifest.json file or if the manifest cannot be read.</exception>
	public static async ValueTask<WorkspaceData> Extract(string packageFile, string? workingDir, bool overwrite, CancellationToken ct)
	{
		if (workingDir is not null && Directory.Exists(workingDir) && overwrite)
		{
			Directory.Delete(workingDir, recursive: true);
		}

		workingDir ??= Path.Combine(Path.GetTempPath(), $"uno-hr-workspace-{Guid.NewGuid():N}");
		var deleteOnError = !Directory.Exists(workingDir);
		try
		{
			Directory.CreateDirectory(workingDir);
			await ZipFile.ExtractToDirectoryAsync(packageFile, workingDir, overwriteFiles: true, ct);

			var manifest = Path.Combine(workingDir, "manifest.json");
			if (!File.Exists(manifest))
			{
				throw new InvalidOperationException("The package is invalid (missing manifest.json).");
			}

			//var packageDir = Path.GetDirectoryName(manifestFile) ?? Directory.GetCurrentDirectory();
			await using var manifestStream = File.OpenRead(manifest);
			var data = await System.Text.Json.JsonSerializer.DeserializeAsync<WorkspaceData>(manifestStream, _jsonOptions, ct)
				?? throw new InvalidOperationException("Unable to read workspace manifest.");

			return new Extractor(workingDir).ExtractWorkspace(data);
		}
		catch (Exception) when (deleteOnError)
		{
			try
			{
				Directory.Delete(workingDir, recursive: true);
			}
			catch (Exception)
			{
				// Best effort
			}

			throw;
		}
	}

	private sealed class Extractor
	{
		private readonly string _baseDirectory;

		public Extractor(string baseDirectory)
		{
			_baseDirectory = baseDirectory;
		}

		public WorkspaceData ExtractWorkspace(WorkspaceData workspace)
			=> workspace with { Solution = ExtractSolution(workspace.Solution) };

		private SolutionData ExtractSolution(SolutionData solution)
			=> solution with
			{
				FilePath = ResolvePath(solution.FilePath),
				Projects = [.. solution.Projects.Select(ExtractProject)],
				AnalyzerReferences = ExtractAnalyzerReferences(solution.AnalyzerReferences)
			};

		private ProjectData ExtractProject(ProjectData project)
			=> project with
			{
				FilePath = ResolvePath(project.FilePath),
				OutputFilePath = ResolvePath(project.OutputFilePath),
				Documents = ExtractDocuments(project.Documents),
				AdditionalDocuments = ExtractDocuments(project.AdditionalDocuments),
				AnalyzerConfigDocuments = ExtractDocuments(project.AnalyzerConfigDocuments),
				MetadataReferences = ExtractMetadataReferences(project.MetadataReferences),
				AnalyzerReferences = ExtractAnalyzerReferences(project.AnalyzerReferences),
				CompilationOutputInfo = project.CompilationOutputInfo is { } info ? ExtractCompilationOutput(info) : project.CompilationOutputInfo
			};

		private ImmutableArray<DocumentData> ExtractDocuments(ImmutableArray<DocumentData> documents)
			=> [.. documents.Select(document => document with { FilePath = ResolvePath(document.FilePath) ?? document.FilePath })];

		private ImmutableArray<MetadataReferenceData> ExtractMetadataReferences(ImmutableArray<MetadataReferenceData> references)
			=> [.. references.Select(reference => reference with { FilePath = ResolvePath(reference.FilePath) ?? reference.FilePath })];

		private ImmutableArray<AnalyzerReferenceData> ExtractAnalyzerReferences(ImmutableArray<AnalyzerReferenceData> references)
			=> [.. references.Select(reference => reference with { FilePath = ResolvePath(reference.FilePath) ?? reference.FilePath })];

		private CompilationOutputInfo ExtractCompilationOutput(CompilationOutputInfo info)
		{
			var output = info;
			if (!string.IsNullOrWhiteSpace(info.AssemblyPath))
			{
				var resolved = ResolvePath(info.AssemblyPath);
				if (resolved is not null)
				{
					output = output.WithAssemblyPath(resolved);
				}
			}

			if (!string.IsNullOrWhiteSpace(info.GeneratedFilesOutputDirectory))
			{
				var resolvedDirectory = ResolvePath(info.GeneratedFilesOutputDirectory);
				if (resolvedDirectory is not null)
				{
					output = output.WithGeneratedFilesOutputDirectory(resolvedDirectory);
				}
			}

			return output;
		}

		private string? ResolvePath(string? path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return path;
			}

			if (Path.IsPathRooted(path))
			{
				return Path.GetFullPath(path);
			}

			return Path.GetFullPath(Path.Combine(_baseDirectory, path));
		}
	}

	private sealed class Packager(string rootDirectory)
	{
		private const string CodeFolder = "code";
		private const string ReferencesFolder = "references";
		private const string AnalyzersFolder = "analyzers";

		private readonly Dictionary<string, CopiedFile> _files = new(StringComparer.OrdinalIgnoreCase);

		public WorkspaceData PackWorkspace(WorkspaceData workspace)
			=> workspace with { Solution = PackSolution(workspace.Solution) };

		private SolutionData PackSolution(SolutionData solution)
			=> solution with
			{
				FilePath = PackCodeFile(solution.FilePath),
				Projects = [.. solution.Projects.Select(PackProject)],
				AnalyzerReferences = [.. solution.AnalyzerReferences.Select(reference => reference with { FilePath = PackAnalyzer(reference.FilePath) })]
			};

		private ProjectData PackProject(ProjectData project)
			=> project with
			{
				FilePath = PackCodeFile(project.FilePath),
				OutputFilePath = PackReferenceFile(project.OutputFilePath),
				Documents = [.. project.Documents.Select(document => document with { FilePath = PackCodeFile(document.FilePath) })],
				AdditionalDocuments = [.. project.AdditionalDocuments.Select(document => document with { FilePath = PackCodeFile(document.FilePath) })],
				AnalyzerConfigDocuments = [.. project.AnalyzerConfigDocuments.Select(document => document with { FilePath = PackAnalyzer(document.FilePath) })],
				MetadataReferences = [.. project.MetadataReferences.Select(reference => reference with { FilePath = PackReferenceFile(reference.FilePath) })],
				AnalyzerReferences = [.. project.AnalyzerReferences.Select(reference => reference with { FilePath = PackAnalyzer(reference.FilePath) })],
				CompilationOutputInfo = PackCompilationOutput(project.CompilationOutputInfo)
			};

		private CompilationOutputInfo PackCompilationOutput(CompilationOutputInfo? info)
		{
			if (info is null)
			{
				return default;
			}

			var output = info.Value;
			output = output.WithAssemblyPath(PackCodeFile(output.AssemblyPath));

			if (!string.IsNullOrWhiteSpace(output.GeneratedFilesOutputDirectory))
			{
				output = output.WithGeneratedFilesOutputDirectory(PackCodeDirectory(output.GeneratedFilesOutputDirectory));
			}

			return output;
		}

		[return: NotNullIfNotNull(nameof(path))]
		private string? PackCodeFile(string? path)
			=> CopyFile(path, CodeFolder)?.RelativePath;

		[return: NotNullIfNotNull(nameof(path))]
		private string? PackReferenceFile(string? path)
			=> CopyFile(path, ReferencesFolder)?.RelativePath;

		[return: NotNullIfNotNull(nameof(path))]
		private string? PackAnalyzer(string? path)
			=> CopyFile(path, AnalyzersFolder)?.RelativePath;

		[return: NotNullIfNotNull(nameof(path))]
		private string? PackCodeDirectory(string? path)
			=> CopyDirectory(path, CodeFolder);

		[return: NotNullIfNotNull(nameof(path))]
		private CopiedFile? CopyFile(string? path, string category)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return null;
			}

			var fullPath = Path.GetFullPath(path);
			if (!File.Exists(fullPath))
			{
				throw new InvalidOperationException($"File '{fullPath}' does not exist and cannot be packaged.");
			}

			if (_files.TryGetValue(fullPath, out var existing))
			{
				return existing;
			}

			var relativePath = BuildPackageRelativePath(fullPath, category);
			var destinationPath = Path.Combine(rootDirectory, relativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
			File.Copy(fullPath, destinationPath, overwrite: true);

			var result = new CopiedFile(relativePath.Replace('\\', '/'), destinationPath);
			_files[fullPath] = result;
			return result;
		}

		[return: NotNullIfNotNull(nameof(sourceDirectory))]
		private string? CopyDirectory(string? sourceDirectory, string category)
		{
			if (string.IsNullOrWhiteSpace(sourceDirectory))
			{
				return null;
			}

			var fullPath = Path.GetFullPath(sourceDirectory);
			if (!Directory.Exists(fullPath))
			{
				throw new InvalidOperationException($"Directory '{fullPath}' does not exist and cannot be packaged.");
			}

			var relativePath = BuildPackageRelativePath(fullPath, category);
			var destinationPath = Path.Combine(rootDirectory, relativePath);
			Directory.CreateDirectory(destinationPath);

			foreach (var directory in Directory.EnumerateDirectories(fullPath, "*", SearchOption.AllDirectories))
			{
				var destinationSubDir = Path.Combine(destinationPath, Path.GetRelativePath(fullPath, directory));
				Directory.CreateDirectory(destinationSubDir);
			}

			foreach (var file in Directory.EnumerateFiles(fullPath, "*", SearchOption.AllDirectories))
			{
				var relativeFilePath = Path.Combine(relativePath, Path.GetRelativePath(fullPath, file));
				var destinationFilePath = Path.Combine(rootDirectory, relativeFilePath);
				Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath)!);
				File.Copy(file, destinationFilePath, overwrite: true);
			}

			return relativePath.Replace('\\', '/');
		}

		private static string BuildPackageRelativePath(string fullPath, string category)
		{
			var root = Path.GetPathRoot(fullPath);
			var relative = root is null
				? Path.GetFileName(fullPath)
				: Path.GetRelativePath(root, fullPath);

			relative = relative
				.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
				.Replace(':', '_');

			return Path.Combine(category, relative);
		}
	}

	private sealed record CopiedFile(string RelativePath, string AbsolutePath);

	private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerOptions.Default)
	{
		WriteIndented = true,
		Converters =
		{
			new JsonStringEnumConverter(),
			new EncodingJsonConverter(),
			new SolutionIdConverter(),
			new ProjectIdConverter(),
			new DocumentIdConverter(),
			new CompilationOptionsConverter(),
			new CompilationOutputInfoConverter(),
			new ParseOptionsConverter(),
			new MetadataReferencePropertiesConverter(),
		}
	};
}
