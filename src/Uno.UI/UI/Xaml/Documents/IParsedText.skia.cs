using Windows.Foundation;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml.Documents;

internal interface IParsedText
{
	void Draw(in Visual.PaintingSession session,
		(int index, CompositionBrush brush)? caret, // null to skip drawing a caret
		(int selectionStart, int selectionEnd, CompositionBrush brush)? selection, // null to skip drawing a selection
		float caretThickness);

	Rect GetRectForIndex(int adjustedIndex);

	int GetIndexAt(Point p, bool ignoreEndingSpace, bool extendedSelection);

	Hyperlink GetHyperlinkAt(Point point);

	/// <param name="right">when on a word boundary, decides whether to return the left or the right word</param>
	(int start, int length) GetWordAt(int index, bool right);

	internal (int start, int length, bool firstLine, bool lastLine, int lineIndex) GetLineAt(int index);
}
