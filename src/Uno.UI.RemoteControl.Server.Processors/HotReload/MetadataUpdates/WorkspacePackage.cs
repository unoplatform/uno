using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Elfie.Model.Tree;
using Uno.UI.RemoteControl.Helpers;
using static System.Net.Mime.MediaTypeNames;

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

	private sealed class Extractor(string baseDirectory)
	{
		public WorkspaceData ExtractWorkspace(WorkspaceData workspace)
			=> workspace with { Solution = ExtractSolution(workspace.Solution) };

		private SolutionData ExtractSolution(SolutionData solution)
			=> solution with
			{
				FilePath = ResolvePath(solution.FilePath),
				Projects = [.. solution.Projects.Select(ExtractProject)],
				AnalyzerReferences = [.. solution.AnalyzerReferences.Select(ExtractAnalyzerReference)],
			};

		private ProjectData ExtractProject(ProjectData project)
		{
			var result = project with
			{
				FilePath = ResolvePath(project.FilePath),
				OutputFilePath = ResolvePath(project.OutputFilePath),
				Documents = [.. project.Documents.Select(ExtractDocument)],
				AdditionalDocuments = [.. project.AdditionalDocuments.Select(ExtractDocument)],
				AnalyzerConfigDocuments = [.. project.AnalyzerConfigDocuments.Select(ExtractDocument)],
				MetadataReferences = [.. project.MetadataReferences.Select(ExtractMetadataReference)],
				AnalyzerReferences = [.. project.AnalyzerReferences.Select(ExtractAnalyzerReference)],
				CompilationOutputInfo = ExtractCompilationOutput(project.CompilationOutputInfo)
			};

			var pathsMap = BuildPathMap(project, ResolvePath);
			foreach (var configDocument in result.AnalyzerConfigDocuments.Where(doc => doc.FilePath is not null))
			{
				PatchPaths(configDocument.FilePath!, pathsMap);
			}

			return result;
		}

		private DocumentData ExtractDocument(DocumentData document)
			=> document with { FilePath = ResolvePath(document.FilePath) };

		private MetadataReferenceData ExtractMetadataReference(MetadataReferenceData document)
			=> document with { FilePath = ResolvePath(document.FilePath) };

		private AnalyzerReferenceData ExtractAnalyzerReference(AnalyzerReferenceData document)
			=> document with { FilePath = ResolvePath(document.FilePath) };

		private CompilationOutputInfo? ExtractCompilationOutput(CompilationOutputInfo? info)
			=> info is { } output
				? output
					.WithAssemblyPath(ResolvePath(output.AssemblyPath))
					.WithGeneratedFilesOutputDirectory(ResolvePath(output.GeneratedFilesOutputDirectory))
				: default;

		[return: NotNullIfNotNull(nameof(path))]
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

			return Path.GetFullPath(Path.Combine(baseDirectory, path));
		}
	}

	private sealed class Packager(string workingDirectory)
	{
		private readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);

		public WorkspaceData PackWorkspace(WorkspaceData workspace)
			=> workspace with { Solution = PackSolution(workspace.Solution) };

		private SolutionData PackSolution(SolutionData solution)
			=> solution with
			{
				FilePath = CopyFile(solution.FilePath),
				Projects = [.. solution.Projects.Select(PackProject)],
				AnalyzerReferences = [.. solution.AnalyzerReferences.Select(reference => reference with { FilePath = CopyFile(reference.FilePath) })]
			};

		private ProjectData PackProject(ProjectData project)
		{
			var pathsMap = BuildPathMap(project, GetRelativePath);
			CopyDirectory(Path.GetDirectoryName(project.OutputFilePath));
			return project with
			{
				FilePath = CopyFile(project.FilePath),
				OutputFilePath = CopyFile(project.OutputFilePath),
				Documents = [.. project.Documents.Select(document => document with { FilePath = CopyFile(document.FilePath) })],
				AdditionalDocuments = [.. project.AdditionalDocuments.Select(document => document with { FilePath = CopyFile(document.FilePath) })],
				AnalyzerConfigDocuments = [.. project.AnalyzerConfigDocuments.Select(document => document with { FilePath = CopyFile(document.FilePath, pathsMap) })],
				MetadataReferences = [.. project.MetadataReferences.Select(reference => reference with { FilePath = CopyFile(reference.FilePath) })],
				AnalyzerReferences = [.. project.AnalyzerReferences.Select(reference => reference with { FilePath = CopyFile(reference.FilePath) })],
				CompilationOutputInfo = PackCompilationOutput(project.CompilationOutputInfo)
			};
		}

		private CompilationOutputInfo? PackCompilationOutput(CompilationOutputInfo? info)
			=> info is { } output
				? output
					.WithAssemblyPath(CopyFile(output.AssemblyPath))
					.WithGeneratedFilesOutputDirectory(CopyDirectory(output.GeneratedFilesOutputDirectory))
				: default;

		[return: NotNullIfNotNull(nameof(path))]
		private string? CopyFile(string? path, IReadOnlyDictionary<string, string>? patchPathsMap = null)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return null;
			}

			var src = Path.GetFullPath(path);
			if (!File.Exists(src))
			{
				throw new InvalidOperationException($"File '{src}' does not exist and cannot be packaged.");
			}

			if (_files.TryGetValue(src, out var existing))
			{
				return existing;
			}

			var rel = GetRelativePath(src);
			var dst = Path.Combine(workingDirectory, rel);
			Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
			File.Copy(src, dst, overwrite: true);

			if (patchPathsMap is { Count: > 0 })
			{
				PatchPaths(dst, patchPathsMap);
			}

			var normalizedPath = rel.Replace('\\', '/');
			_files[src] = normalizedPath;
			return normalizedPath;
		}

		[return: NotNullIfNotNull(nameof(sourceDirectory))]
		private string? CopyDirectory(string? sourceDirectory)
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

			var rel = GetRelativePath(fullPath);
			var dst = Path.Combine(workingDirectory, rel);
			Directory.CreateDirectory(dst);

			foreach (var directory in Directory.EnumerateDirectories(fullPath, "*", SearchOption.AllDirectories))
			{
				var destinationSubDir = Path.Combine(dst, Path.GetRelativePath(fullPath, directory));
				Directory.CreateDirectory(destinationSubDir);
			}

			foreach (var file in Directory.EnumerateFiles(fullPath, "*", SearchOption.AllDirectories))
			{
				var fileSrc = Path.GetFullPath(file);
				if (_files.ContainsKey(fileSrc))
				{
					continue;
				}

				var fileRel = Path.Combine(rel, Path.GetRelativePath(fullPath, file));
				var fileDst = Path.Combine(workingDirectory, fileRel);
				Directory.CreateDirectory(Path.GetDirectoryName(fileDst)!);
				File.Copy(fileSrc, fileDst, overwrite: true);

				_files[fileSrc] = fileRel.Replace('\\', '/');
			}

			return rel.Replace('\\', '/');
		}

		[return: NotNullIfNotNull(nameof(fullPath))]
		private static string? GetRelativePath(string? fullPath)
		{
			if (fullPath is null)
			{
				return null;
			}

			var root = Path.GetPathRoot(fullPath);
			var relative = root is null
				? Path.GetFileName(fullPath)
				: Path.GetRelativePath(root, fullPath);

			relative = relative.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

			if (!string.IsNullOrWhiteSpace(root))
			{
				var sanitizedRoot = root
					.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
					.Replace(':', '_')
					.Replace('\\', '_')
					.Replace('/', '_');

				relative = Path.Combine(sanitizedRoot, relative);
			}

			return relative.Replace(':', '_').Replace('\\', '/');
		}
	}

	private static void PatchPaths(string path, IReadOnlyDictionary<string, string> pathsMap)
	{
		var text = File.ReadAllText(path);
		var updatedText = new StringBuilder(Math.Min(text.Length + pathsMap.Count * 128, 4096));
		var normalizedText = text.Replace('\\', '/');
		var replacements = pathsMap
			.Select((pair, priority) => new Replacement(normalizedText, priority, pair.Key, pair.Value, _pathComparison))
			.ToList();

		var updated = false;
		var shift = 0;
		var index = 0;
		while (index < text.Length)
		{
			var replacement = replacements
				.Where(r => r.MoveNext(index))
				.OrderBy(r => r.CurrentIndex)
				.ThenBy(r => r.Priority)
				.FirstOrDefault(Replacement.Null);
			if (Replacement.Null.Equals(replacement))
			{
				break;
			}

			updated = true;
			updatedText.Append(text.AsSpan()[index..replacement.CurrentIndex]);
			updatedText.Append(replacement.NewValue.AsSpan());
			index = replacement.CurrentIndex + replacement.OldValue.Length;
		}

		if (updated)
		{
			updatedText.Append(text.AsSpan()[(index + shift)..]);
			File.WriteAllText(path, updatedText.ToString());
		}

		//static IEnumerable<int> IndexOf(string text, int start, Dictionary<string, string> replacements, [NotNullWhen(true)] out KeyValuePair<string, string>? replacement)
		//{
		//	replacements.Where()

		//	foreach (var r in replacements)
		//	{
		//		var index = text.IndexOf(r.Key, start);
		//		if ( is >0)
		//	}
		//}

		//static IEnumerable<int> IndicesOf(string text, string value, StringComparison comparison)
		//{
		//	var index = 0;
		//	while ((index = text.IndexOf(value, index, comparison)) > 0)
		//	{
		//		yield return index;
		//	}
		//}
	}

	private record struct Replacement
	{
		private readonly IEnumerator<int> _indices;
		private bool _hasIndex;

		public static readonly Replacement Null = new Replacement(string.Empty, int.MinValue, string.Empty, string.Empty, StringComparison.Ordinal);

		public Replacement(string text, int priority, string oldValue, string newValue, StringComparison comparison)
		{
			Priority = priority;
			OldValue = oldValue;
			NewValue = newValue;
			Shift = newValue.Length - oldValue.Length;

			_indices = IndicesOf(text, oldValue, comparison);
			_hasIndex = _indices.MoveNext();
		}

		public int CurrentIndex => _hasIndex ? _indices.Current : -1;

		public int Priority { get; }

		public int Shift { get; }

		public string OldValue { get; }

		public string NewValue { get; }

		public bool MoveNext(int startIndex)
		{
			while (_hasIndex)
			{
				if (_indices.Current >= startIndex)
				{
					return true;
				}

				_hasIndex = _indices.MoveNext();
			}

			return false;
		}
	}

	static IEnumerator<int> IndicesOf(string text, string value, StringComparison comparison)
	{
		var index = 0;
		while ((index = text.IndexOf(value, index, comparison)) > 0)
		{
			yield return index;
			index += value.Length;
		}
	}

	[return: NotNullIfNotNull(nameof(path))]
	private delegate string? PathTransformation(string? path);

	private static IReadOnlyDictionary<string, string> BuildPathMap(ProjectData project, PathTransformation transformation)
	{
		var map = new Dictionary<string, string>(_pathComparer);
		
		// First we add exact full path of documents that will allow us to replace the complete path. not only the base dir
		Add(project.Documents);
		Add(project.AdditionalDocuments);

		// Then, as a fallback, we add the base dir of the project to attempt to patch unknown path.
		// WARNING: This means that some paths may be mixing `\` and `/` which could drive to undefined behavior
		//			when building and using package on a different OS, but is usually a better solution as modern OS supports both.
		if (Path.GetDirectoryName(project.FilePath) is { } projectDir)
		{
			map[projectDir.Replace('\\', '/')] = transformation(projectDir);
		}

		return map;

		void Add(ImmutableArray<DocumentData> documents)
		{
			foreach (var document in documents)
			{
				if (document.FilePath is not null)
				{
					map[document.FilePath.Replace('\\', '/')] = transformation(document.FilePath);
				}
			}
		}
	}

	private static readonly StringComparison _pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
	private static readonly IEqualityComparer<string> _pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

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
