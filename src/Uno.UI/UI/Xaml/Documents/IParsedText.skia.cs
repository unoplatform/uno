using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;

namespace Microsoft.UI.Xaml.Documents;

internal interface IParsedText
{
	void Draw(in Visual.PaintingSession session,
		(int index, CompositionBrush brush, float thickness)? caret, // null to skip drawing a caret
		IEnumerable<TextHighlighter> highlighters,
		(int startIndex, int length)? compositionRange
	);

	Rect GetRectForIndex(int adjustedIndex);

	int GetIndexAt(Point p, bool ignoreEndingNewLine, bool extendedSelection);

	Hyperlink GetHyperlinkAt(Point point);

	/// <param name="right">when on a word boundary, decides whether to return the left or the right word</param>
	(int start, int length) GetWordAt(int index, bool right);

	internal (int start, int length, bool firstLine, bool lastLine, int lineIndex) GetLineAt(int index);

	bool IsBaseDirectionRightToLeft { get; }

	/// <summary>
	/// Draws the spell-check squiggles laid out by the last <see cref="Draw"/>, in the text's own coordinates.
	/// <see cref="Draw"/> leaves them out: they sit below the line box, where the text's own clip would cut them, so the
	/// owning TextBox draws them in a layer of its own.
	/// </summary>
	void DrawSpellCheckSquiggles(SKCanvas canvas, float opacity);
}
