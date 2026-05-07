using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Uno.HotReload.Diffing;

/// <summary>
/// Extension helpers over <see cref="ChangeSet"/>.
/// </summary>
public static class ChangeSetExtensions
{
	private static readonly SolutionUpdater _default = new();

	/// <summary>
	/// Backwards-compat shim over <see cref="SolutionUpdater"/>. New code should
	/// depend on <see cref="ISolutionUpdater"/> directly so it can also observe
	/// <see cref="SolutionUpdateResult.IgnoredChanges"/> and
	/// <see cref="SolutionUpdateResult.Diagnostics"/>.
	/// </summary>
	public static async ValueTask<Solution> ApplyAsync(this Solution solution, ChangeSet changeSet, CancellationToken ct)
	{
		var result = await _default.UpdateAsync(solution, changeSet, ct).ConfigureAwait(false);
		return result.Solution;
	}
}
