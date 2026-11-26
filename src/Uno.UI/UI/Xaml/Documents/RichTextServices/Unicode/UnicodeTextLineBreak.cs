#nullable enable

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// The continuation token between <see cref="TextFormatter.FormatLine"/> calls: the layout and the engine line that
/// comes next. A node on the next page resumes a layout of its own at <see cref="NextEngineStart"/>, the index where
/// this layout's next line starts, rather than at a node index, which can't tell the two sides of an object apart.
/// </summary>
internal sealed class UnicodeTextLineBreak : TextLineBreak
{
	public UnicodeTextLineBreak(TextSource textSource, UnicodeParagraphLayout layout, double wrappingWidth, int nextEngineLine, int nextEngineStart)
	{
		TextSource = textSource;
		Layout = layout;
		WrappingWidth = wrappingWidth;
		NextEngineLine = nextEngineLine;
		NextEngineStart = nextEngineStart;
	}

	public TextSource TextSource { get; }

	public UnicodeParagraphLayout Layout { get; }

	public double WrappingWidth { get; }

	public int NextEngineLine { get; }

	public int NextEngineStart { get; }
}
