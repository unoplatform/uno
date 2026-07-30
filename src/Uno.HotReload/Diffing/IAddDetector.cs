using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.HotReload.Diffing;

/// <summary>
/// Detects files that have been added to the project but are not yet part of the current solution.
/// </summary>
public interface IAddDetector
{
	/// <summary>
	/// Discovers which of the given <paramref name="newFiles"/> belong to a project and classifies them as documents or additional documents.
	/// </summary>
	ValueTask<AddDetectionResult> DiscoverAddsAsync(ImmutableHashSet<string> newFiles, CancellationToken ct);
}
