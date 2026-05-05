#nullable enable

using System.Runtime.CompilerServices;
using Uno.Foundation.Logging;

namespace Uno.HotReload.Client;

/// <summary>
/// Public entry point to the transactional, per-caller, phase-scoped hot-reload UI pause
/// mechanism. Replaces the legacy ambient <c>Uno.UI.Helpers.TypeMappings.Pause()</c> flag.
/// </summary>
/// <remarks>
/// The API is public: any caller may acquire a pause when it needs to defer visual-tree
/// apply for a window of work it owns end-to-end. The caller is responsible for managing
/// the handle lifetime, dropping the right types, and disposing the handle. In Uno itself
/// the only in-tree caller is <c>ClientHotReloadProcessor.UpdateFile</c>.
/// </remarks>
public static class UIUpdate
{
	private static readonly Logger _log = typeof(UIUpdate).Log();

	/// <summary>
	/// Acquires a pause covering <paramref name="phases"/>. Multiple concurrent handles are
	/// supported — each phase is counted independently. All phases are unpaused (and pending
	/// types drained) when the last handle pausing any phase is disposed.
	/// </summary>
	/// <param name="phases">Phases to pause. Defaults to <see cref="HotReloadUIPhases.All"/>.</param>
	/// <param name="reason">
	/// Optional human-readable reason surfaced in diagnostic logs when types are deferred or
	/// dropped. Prefer a short noun phrase, e.g. <c>"UpdateFile – MyPage.xaml"</c>.
	/// </param>
	/// <param name="caller">Captured automatically — the calling member name.</param>
	/// <param name="filePath">Captured automatically — the calling source file.</param>
	/// <param name="line">Captured automatically — the calling source line.</param>
	public static HotReloadUIPauseHandle Pause(
		HotReloadUIPhases phases = HotReloadUIPhases.All,
		string? reason = null,
		[CallerMemberName] string? caller = null,
		[CallerFilePath] string? filePath = null,
		[CallerLineNumber] int line = 0)
	{
		var handle = new HotReloadUIPauseHandle(phases, caller, filePath, line, reason);
		PendingUIUpdates.Acquire(handle);

		if (_log.IsEnabled(LogLevel.Trace))
		{
			var reasonPart = reason is { Length: > 0 } ? $" reason='{reason}'" : string.Empty;
			_log.Trace($"[HotReload UI Pause] Acquired phases={phases}{reasonPart} caller='{caller}@{line}'");
		}

		return handle;
	}

	/// <summary>
	/// True if at least one handle is currently pausing any phase named by <paramref name="phase"/>.
	/// </summary>
	public static bool IsPaused(HotReloadUIPhases phase)
		=> PendingUIUpdates.IsPaused(phase);

	/// <summary>
	/// Human-readable summary of all currently-active pause handles, for diagnostics.
	/// </summary>
	public static string GetPauseHoldersSummary()
		=> PendingUIUpdates.GetPauseHoldersSummary();
}
