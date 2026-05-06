using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Tracking;

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

	/// <summary>
	/// Translates an <see cref="SolutionUpdateResult.IgnoredChanges"/> set into
	/// <see cref="HotReloadOperation.NotifyIgnored(string)"/> calls so the
	/// operation report records every input the cycle did not act on. Updaters
	/// build the ignored set with a record-<c>with</c> expression so any new
	/// <see cref="ChangeSet"/> field they don't explicitly consume flows through
	/// here automatically.
	/// </summary>
	public static void NotifyIgnored(this HotReloadOperation hotReload, ChangeSet ignored)
	{
		if (ignored.IgnoredFiles.Count > 0)
		{
			hotReload.NotifyIgnored(ignored.IgnoredFiles);
		}

		foreach (var added in ignored.AddedDocuments)
		{
			if (added.Document.FilePath is { Length: > 0 } path)
			{
				hotReload.NotifyIgnored(path);
			}
		}

		foreach (var added in ignored.AddedAdditionalDocuments)
		{
			if (added.Document.FilePath is { Length: > 0 } path)
			{
				hotReload.NotifyIgnored(path);
			}
		}

		foreach (var edited in ignored.EditedDocuments)
		{
			if (edited.FilePath is { Length: > 0 } path)
			{
				hotReload.NotifyIgnored(path);
			}
		}

		foreach (var edited in ignored.EditedAdditionalDocuments)
		{
			if (edited.FilePath is { Length: > 0 } path)
			{
				hotReload.NotifyIgnored(path);
			}
		}

		foreach (var editedProject in ignored.EditedProjects)
		{
			if (editedProject.FilePath is { Length: > 0 } path)
			{
				hotReload.NotifyIgnored(path);
			}
		}

		foreach (var addedProject in ignored.AddedProjects)
		{
			if (addedProject.FilePath is { Length: > 0 } path)
			{
				hotReload.NotifyIgnored(path);
			}
		}
	}
}
