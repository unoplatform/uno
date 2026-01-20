using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Documents;

internal interface IParsedText
{
	void Draw(in Visual.PaintingSession session,
		(int index, CompositionBrush brush, float thickness)? caret, // null to skip drawing a caret
		IList<TextHighlighter> highlighters);

	Rect GetRectForIndex(int adjustedIndex);

	int GetIndexAt(Point p, bool ignoreEndingNewLine, bool extendedSelection);

	Hyperlink GetHyperlinkAt(Point point);

	/// <param name="right">when on a word boundary, decides whether to return the left or the right word</param>
	(int start, int length) GetWordAt(int index, bool right);

	internal (int start, int length, bool firstLine, bool lastLine, int lineIndex) GetLineAt(int index);

	bool IsBaseDirectionRightToLeft { get; }
}
