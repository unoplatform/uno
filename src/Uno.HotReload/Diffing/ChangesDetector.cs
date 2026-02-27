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
	/// <inheritdoc />
	public async ValueTask<ChangeSet> DiscoverChangesAsync(Solution solution, ImmutableHashSet<string> files, CancellationToken ct)
	{
		var editedDocuments = new List<Document>();
		var editedAdditionalDocuments = new List<TextDocument>();
		var removedDocuments = new List<DocumentId>();
		var removedAdditionalDocuments = new List<DocumentId>();
		var potentiallyAdded = new List<string>();
		var notFound = new List<string>();

		foreach (var file in files)
		{
			var found = false;
			var exists = File.Exists(file);
			var documents = solution
				.Projects
				.SelectMany(p => p.Documents)
				.Where(d => string.Equals(d.FilePath, file, PathComparer.Comparison));
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
				.Where(d => string.Equals(d.FilePath, file, PathComparer.Comparison));
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
			[.. notFound, .. added.Ignored]);
	}
}
