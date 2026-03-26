namespace Microsoft.UI.Xaml;

/// <summary>
/// Global generation counter for text formatting inheritance.
/// All <see cref="TextFormatting"/> objects compare their local counter
/// against this global value to detect staleness.
/// </summary>
/// <remarks>
/// Mirrors WinUI's <c>CCoreServices::m_cInheritedPropGenerationCounter</c>
/// (corep.h:2116).
///
/// Incremented when:
/// <list type="bullet">
///   <item>Any text formatting property changes on any element</item>
///   <item>An element enters or leaves the visual tree</item>
///   <item>Foreground is frozen/unfrozen at a theme boundary</item>
/// </list>
///
/// When the counter changes, all <see cref="TextFormatting.IsOld"/> checks
/// return <see langword="true"/>, causing the next
/// <see cref="UIElement.EnsureTextFormatting"/> call to trigger
/// <see cref="UIElement.PullInheritedTextFormatting"/>.
/// </remarks>
internal static class GlobalTextFormattingCounter
{
	/// <summary>
	/// The current global counter value.
	/// </summary>
	internal static uint Value;

	/// <summary>
	/// Increments the counter, marking all <see cref="TextFormatting"/>
	/// instances as potentially stale.
	/// MUX ref: <c>CDependencyObject::InvalidateInheritedProperty()</c>
	/// — depends.cpp:2706-2731.
	/// </summary>
	internal static void Invalidate() => Value++;
}
