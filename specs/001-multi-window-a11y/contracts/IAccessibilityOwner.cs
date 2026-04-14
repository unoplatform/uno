#nullable enable

// CONTRACT — Phase 1 design artifact.
// This file is not compiled in-place; the shipped implementation lives at
// src/Uno.UI.Runtime.Skia/Accessibility/IAccessibilityOwner.cs.
//
// Feature: 001-multi-window-a11y
// Consumers: Win32WindowWrapper (PR 1), MacOSWindowHost (PR 2)

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
	/// </summary>
	/// <remarks>
	/// Non-null after the wrapper's accessibility initialization step has run.
	/// May become null (or the instance becomes disposed) during the wrapper's
	/// window-destroy sequence; callers must tolerate null.
	/// </remarks>
	SkiaAccessibilityBase? Accessibility { get; }
}
