#nullable enable

using System.Collections.Generic;

namespace SamplesApp.AppiumTests.Infrastructure;

/// <summary>
/// Platform-neutral snapshot of a single accessibility element. The full tree
/// is represented by linking these via <see cref="Children"/>. Persisted to
/// JSON by <see cref="SnapshotSerializer"/>; compared by
/// <see cref="SnapshotComparer"/>.
/// </summary>
public sealed class AccessibilityNode
{
	/// <summary>Canonical role (lowercased, e.g. button/text/edit/group).</summary>
	public string Role { get; set; } = string.Empty;

	/// <summary>Best accessible name the platform exposes. Empty string when none.</summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// AutomationId equivalent: <see cref="Microsoft.UI.Xaml.Automation.AutomationProperties.AutomationIdProperty"/>
	/// on Win32 / WASM, AXIdentifier on macOS. Empty string when none.
	/// </summary>
	public string AutomationId { get; set; } = string.Empty;

	/// <summary>Value-pattern string. Null when the element has no value pattern.</summary>
	public string? Value { get; set; }

	/// <summary>
	/// Canonical names of supported automation patterns (invoke, toggle,
	/// value, rangevalue, expandcollapse, selection, selectionitem).
	/// Sorted, lower-cased.
	/// </summary>
	public List<string> Patterns { get; set; } = new();

	/// <summary>
	/// Platform-specific raw attributes kept around so diff output stays
	/// debuggable even when normalization hides differences. Keys are
	/// "&lt;flavor&gt;.&lt;attr&gt;" (e.g. "win32.LocalizedControlType").
	/// </summary>
	public Dictionary<string, string> Extras { get; set; } = new();

	public List<AccessibilityNode> Children { get; set; } = new();
}
