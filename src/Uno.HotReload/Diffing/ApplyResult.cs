using Microsoft.CodeAnalysis;

namespace Uno.HotReload.Diffing;

/// <summary>
/// Outcome of an <see cref="IHotReloadChangeApplier.ApplyChangesAsync"/> call.
/// </summary>
/// <param name="Solution">
/// The solution that the manager should keep working with. May be the original
/// solution unchanged when <paramref name="IsCompleted"/> is <c>true</c>.
/// </param>
/// <param name="IsCompleted">
/// When <c>true</c>, the applier has already completed the
/// <see cref="Tracking.HotReloadOperation"/> (typically with
/// <see cref="Tracking.HotReloadOperationResult.Failed"/>) and the manager
/// must skip <c>EmitSolutionUpdateAsync</c>, the "no changes" log, and the
/// trailing <c>Complete(NoChanges)</c> call. When <c>false</c>, the manager
/// runs the standard cycle on top of <paramref name="Solution"/>.
/// </param>
public sealed record ApplyResult(Solution Solution, bool IsCompleted);
