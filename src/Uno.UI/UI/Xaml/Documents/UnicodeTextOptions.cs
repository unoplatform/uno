#nullable enable

namespace Microsoft.UI.Xaml.Documents;

/// <summary>
/// Optional inputs to a <see cref="UnicodeText"/> layout. The default value changes nothing, so callers that don't set
/// it lay out exactly as before.
/// </summary>
internal readonly record struct UnicodeTextOptions
{
	/// <summary>
	/// The index where the visible layout resumes, for text that a previous layout of the same paragraph broke at its
	/// own width. 0 lays out from the start.
	/// </summary>
	/// <remarks>
	/// The bidi resolution and the shaping still cover the whole text, so the resumed lines keep the paragraph's level
	/// and the joining across the break. The text before the index is laid out on lines that never wrap and end at the
	/// index; <see cref="UnicodeText.FirstResumedLine"/> is the first line after them. The index must be a cluster
	/// boundary, which a line start of the previous layout built from the same inlines always is; the constructor throws
	/// otherwise. Those prefix lines are never trimmed, and the line count and height limits (maxLines, the available
	/// height) and the calculated size count them too, so a resumed layout passes 0 and infinity for those, reads the
	/// metrics of the lines from <see cref="UnicodeText.FirstResumedLine"/> on, and leaves paging to its caller.
	/// </remarks>
	public int ResumeIndex { get; init; }
}
