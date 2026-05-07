using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Tracking;
using Uno.HotReload.Utils;

namespace Uno.HotReload.Diffing;

/// <summary>
/// Default implementation of <see cref="IChangesDetector"/> that classifies file changes into edits, removes, and adds
/// by comparing them against the current solution and delegating add detection to an <see cref="IAddDetector"/>.
/// </summary>
public class ChangesDetector(IAddDetector addDetector, IReporter reporter) : IChangesDetector
{
	/// <summary>
	/// Filenames that participate in csproj evaluation by MSBuild convention. When any of
	/// these files is edited (anywhere in the project's parent chain), the corresponding
	/// project is flagged on <see cref="ChangeSet.EditedProjects"/> so wrapping appliers
	/// can re-evaluate the csproj.
	/// </summary>
	private static readonly string[] _csprojInfluencingFileNames =
	[
		"Directory.Build.props",
		"Directory.Build.targets",
		"Directory.Packages.props",
	];

	/// <inheritdoc />
	public async ValueTask<ChangeSet> DiscoverChangesAsync(Solution solution, ImmutableHashSet<string> files, CancellationToken ct)
	{
		var editedDocuments = new List<Document>();
		var editedAdditionalDocuments = new List<TextDocument>();
		var removedDocuments = new List<DocumentId>();
		var removedAdditionalDocuments = new List<DocumentId>();
		var editedProjects = new List<Project>();
		var potentiallyAdded = new List<string>();
		var notFound = new List<string>();

		foreach (var file in files)
		{
			var found = false;
			var exists = File.Exists(file);
			var documents = solution
				.Projects
				.SelectMany(p => p.Documents)
				.Where(d => PathComparer.PathEquals(d.FilePath, file));
			foreach (var document in documents)
			{
				found = true;
				if (exists)
				{
					editedDocuments.Add(document);
				}
				else
				{
					removedDocuments.Add(document.Id);
				}
			}

			var additionalDocuments = solution
				.Projects
				.SelectMany(p => p.AdditionalDocuments)
				.Where(d => PathComparer.PathEquals(d.FilePath, file));
			foreach (var additionalDocument in additionalDocuments)
			{
				found = true;
				if (exists)
				{
					editedAdditionalDocuments.Add(additionalDocument);
				}
				else
				{
					removedAdditionalDocuments.Add(additionalDocument.Id);
				}
			}

			// Match the file against project-level inputs (csproj or its
			// MSBuild-conventional siblings). A match flags the project for
			// downstream re-evaluation but does not consume the file — the
			// applier owns the re-read.
			foreach (var project in solution.Projects)
			{
				if (project.FilePath is not { Length: > 0 } projectPath)
				{
					continue;
				}

				if (PathComparer.PathEquals(projectPath, file)
					|| IsCsprojInfluencingFile(file, projectPath))
				{
					editedProjects.Add(project);
					found = true;
				}
			}

			// Not found in current solution
			if (!found)
			{
				if (exists)
				{
					potentiallyAdded.Add(file);
				}
				else
				{
					reporter.Verbose($"Could not find document with path '{file}' in the workspace and file does not exist on disk.");
					notFound.Add(file);
				}
			}
		}

		var added = await addDetector.DiscoverAddsAsync(ImmutableHashSet.CreateRange(potentiallyAdded), ct).ConfigureAwait(false);

		return new(
			[.. editedDocuments],
			[.. editedAdditionalDocuments],
			added.Documents,
			added.AdditionalDocuments,
			[.. removedDocuments],
			[.. removedAdditionalDocuments],
			[.. editedProjects],
			AddedProjects: [],
			RemovedProjects: [],
			[.. notFound, .. added.Ignored]);
	}

	/// <summary>
	/// Returns <c>true</c> when <paramref name="file"/> is a <c>Directory.Build.props</c>,
	/// <c>Directory.Build.targets</c>, or <c>Directory.Packages.props</c> located in any
	/// directory along the chain from the project's folder up to the filesystem root.
	/// </summary>
	private static bool IsCsprojInfluencingFile(string file, string projectPath)
	{
		var fileName = Path.GetFileName(file);
		var matchesWellKnown = false;
		foreach (var wellKnown in _csprojInfluencingFileNames)
		{
			if (string.Equals(fileName, wellKnown, PathComparer.Comparison))
			{
				matchesWellKnown = true;
				break;
			}
		}

		if (!matchesWellKnown)
		{
			return false;
		}

		var fileDirectory = PathComparer.Normalize(Path.GetDirectoryName(file));
		if (string.IsNullOrEmpty(fileDirectory))
		{
			return false;
		}

		var projectDirectory = PathComparer.Normalize(Path.GetDirectoryName(projectPath));
		while (!string.IsNullOrEmpty(projectDirectory))
		{
			if (string.Equals(projectDirectory, fileDirectory, PathComparer.Comparison))
			{
				return true;
			}

			projectDirectory = PathComparer.Normalize(Path.GetDirectoryName(projectDirectory));
		}

		return false;
	}
}
