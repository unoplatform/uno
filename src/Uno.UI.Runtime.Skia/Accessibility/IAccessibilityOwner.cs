#nullable enable

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// Capability exposed by a per-host window wrapper so the process-wide
/// <see cref="AccessibilityRouter"/> can reach the wrapper's per-window
/// accessibility instance.
/// </summary>
/// <remarks>
/// Resolution path from an arbitrary accessibility callback:
///   peer/element → UIElement.XamlRoot → XamlRootMap.GetHostForRoot(...)
///     → (IAccessibilityOwner)host → Accessibility
/// </remarks>
internal interface IAccessibilityOwner
{
	/// <summary>
	/// The per-window accessibility instance.
	/// Non-null after the wrapper's accessibility initialization step has run.
	/// May become null (or the instance becomes disposed) during the wrapper's
	/// window-destroy sequence; callers must tolerate null.
	/// </summary>
	SkiaAccessibilityBase? Accessibility { get; }
}
