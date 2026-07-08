using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Tracking;
using Uno.HotReload.Utils;

namespace Uno.HotReload.Diffing;

/// <summary>
/// Discovers added files by creating a temporary <see cref="Workspace"/> and looking up the new files in its solution.
/// </summary>
public class TemporaryWorkspaceAddDetector : IAddDetector
{
	private readonly Func<CancellationToken, ValueTask<Solution>> _createSolution;
	private readonly IReporter _reporter;

	public TemporaryWorkspaceAddDetector(Func<CancellationToken, ValueTask<Workspace>> createWorkspace, IReporter reporter)
		: this(async ct => (await createWorkspace(ct).ConfigureAwait(false)).CurrentSolution, reporter)
	{
	}

	/// <summary>
	/// Creates a detector from a solution provider — the provider returns the solution snapshot
	/// to search (typically the same provider used to initialize the hot-reload manager, so the
	/// temporary lookup honors the same target-framework restriction); the snapshot's
	/// originating <see cref="Solution.Workspace"/> is disposed once the lookup completes.
	/// </summary>
	public TemporaryWorkspaceAddDetector(Func<CancellationToken, ValueTask<Solution>> createSolution, IReporter reporter)
	{
		_createSolution = createSolution;
		_reporter = reporter;
	}

	/// <inheritdoc />
	public async ValueTask<AddDetectionResult> DiscoverAddsAsync(ImmutableHashSet<string> newFiles, CancellationToken ct)
	{
		if (newFiles is not { IsEmpty: false })
		{
			return new([], [], newFiles);
		}

		Solution? tempSolution = null;
		try
		{
			_reporter.Output($"Detected {newFiles.Count} potentially new file(s). Creating temporary workspace to discover them...");

			// Create a temporary workspace to discover the new files
			tempSolution = await _createSolution(ct).ConfigureAwait(false);

			var discoveredDocuments = ImmutableArray.CreateBuilder<AddedDocumentInfo>();
			var discoveredAdditionalDocuments = ImmutableArray.CreateBuilder<AddedDocumentInfo>();
			var ignoredFiles = ImmutableHashSet.CreateBuilder<string>();

			foreach (var file in newFiles)
			{
				// Search for the file in the temp workspace's projects
				// Note: Here again we assume that document can appear in more than one project (same project loaded with different TFM)
				var found = false;
				foreach (var project in tempSolution.Projects)
				{
					if (project.Documents.FirstOrDefault(d => PathComparer.PathEquals(d.FilePath, file)) is { } document)
					{
						found = true;
						discoveredDocuments.Add(new(project.GetInfo(), document.GetInfo()));
					}
					else if (project.AdditionalDocuments.FirstOrDefault(d => PathComparer.PathEquals(d.FilePath, file)) is { } additionalDocument)
					{
						found = true;
						discoveredAdditionalDocuments.Add(new(project.GetInfo(), additionalDocument.GetInfo()));
					}
				}

				if (!found)
				{
					ignoredFiles.Add(file);
				}
			}

			return new(discoveredDocuments.ToImmutable(), discoveredAdditionalDocuments.ToImmutable(), ignoredFiles.ToImmutable());
		}
		catch (Exception ex)
		{
			_reporter.Warn($"Error while discovering new files: {ex.Message}");
			return new([], [], newFiles);
		}
		finally
		{
			tempSolution?.Workspace.Dispose();
		}
	}
}
