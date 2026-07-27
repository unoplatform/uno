#nullable enable

namespace Microsoft.UI.Text;

/// <summary>
/// Provides access to the text, selection, formatting, clipboard, stream, and undo operations of a rich text document.
/// </summary>
/// <remarks>
/// Uno keeps the managed document, selection, and range objects stable while callers retain them,
/// and implements the public WinUI text-object-model behavior on those managed objects. Private
/// RichEdit COM details are not portable: Uno does not expose ITextDocument2/ITextRange2 identity,
/// QueryInterface identity cookies, COM apartment or marshaling behavior, or COM reference counts.
/// Code should depend on the public WinUI interfaces and observable range behavior instead.
/// </remarks>
public partial interface ITextDocument
{
	CaretType CaretType { get; set; }

	float DefaultTabStop { get; set; }

	ITextSelection Selection { get; }

	uint UndoLimit { get; set; }

	bool CanCopy();

	bool CanPaste();

	bool CanRedo();

	bool CanUndo();

	int ApplyDisplayUpdates();

	int BatchDisplayUpdates();

	void BeginUndoGroup();

	void EndUndoGroup();

	ITextCharacterFormat GetDefaultCharacterFormat();

	ITextParagraphFormat GetDefaultParagraphFormat();

	ITextRange GetRange(int startPosition, int endPosition);

	ITextRange GetRangeFromPoint(global::Windows.Foundation.Point point, PointOptions options);

	void GetText(TextGetOptions options, out string value);

	void LoadFromStream(TextSetOptions options, global::Windows.Storage.Streams.IRandomAccessStream value);

	void Redo();

	void SaveToStream(TextGetOptions options, global::Windows.Storage.Streams.IRandomAccessStream value);

	void SetDefaultCharacterFormat(ITextCharacterFormat value);

	void SetDefaultParagraphFormat(ITextParagraphFormat value);

	void SetText(TextSetOptions options, string value);

	void Undo();
}
