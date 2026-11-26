#nullable enable

using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// What <see cref="UnicodeTextFormatter"/> reads from a <see cref="TextSource"/>. WinUI's formatter pulls one run at a
/// time through GetTextRun; <see cref="UnicodeText"/> lays a whole paragraph out from its leaf inlines.
/// </summary>
internal interface IUnicodeParagraphSource
{
	// The pre-order leaf inlines of the paragraph.
	Inline[] GetLeafInlines();

	// Told when a font the layout needed finishes loading. For an overflow it is the master, which owns the content.
	UnicodeText.IFontCacheUpdateListener FontListener { get; }

	// The foreground of the content owner, for the empty line after a trailing line break.
	Brush? DefaultForeground { get; }

	// Changes whenever a brush in the content changes: a layout takes the new brushes before it draws.
	int ForegroundVersion { get; }

	// Whether an embedded element can be (re)hosted now: true only while the paragraph measures.
	bool CanHostElements { get; }

	// Creates and formats the ObjectRun of an InlineUIContainer, measuring its child against the embedded element host.
	(ObjectRun Run, ObjectRunMetrics Metrics)? FormatInlineObject(InlineUIContainer container, float paragraphWidth);
}
