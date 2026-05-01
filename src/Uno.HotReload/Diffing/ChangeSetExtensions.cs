using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Tracking;

namespace Uno.HotReload.Diffing;

/// <summary>
/// Backwards-compat shim over <see cref="DefaultHotReloadChangeApplier"/>. New
/// code should depend on <see cref="IHotReloadChangeApplier"/> directly.
/// </summary>
public static class ChangeSetExtensions
{
	private static readonly DefaultHotReloadChangeApplier _default = new();

	public static async ValueTask<Solution> ApplyAsync(this Solution solution, ChangeSet changeSet, HotReloadOperation hotReload, CancellationToken ct)
	{
		var result = await _default.ApplyChangesAsync(solution, changeSet, hotReload, ct).ConfigureAwait(false);
		return result.Solution;
	}
}
