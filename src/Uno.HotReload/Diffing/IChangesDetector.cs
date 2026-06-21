using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Uno.HotReload.Diffing;

/// <summary>
/// Detects changes (edits, adds, and removes) in a set of files against a given solution.
/// </summary>
public interface IChangesDetector
{
	/// <summary>
	/// Analyzes the given <paramref name="files"/> against the <paramref name="solution"/> and returns a <see cref="ChangeSet"/> describing all detected changes.
	/// </summary>
	ValueTask<ChangeSet> DiscoverChangesAsync(Solution solution, ImmutableHashSet<string> files, CancellationToken ct);
}
