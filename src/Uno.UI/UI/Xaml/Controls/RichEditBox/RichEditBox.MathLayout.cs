#nullable enable

using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Documents;

namespace Microsoft.UI.Xaml.Controls;

public partial class RichEditBox
{
	private MathDocument? _mathLayoutDocument;
	private MathTextLayoutSource? _mathLayoutSource;

	private ICustomTextLayout? GetMathLayout(MathDocument? document)
	{
		if (!ReferenceEquals(_mathLayoutDocument, document))
		{
			_mathLayoutDocument = document;
			_mathLayoutSource = document is null ? null : new MathTextLayoutSource(document);
		}

		return _mathLayoutSource;
	}

	internal int MathIndexStorageByteCount
		=> _textBoxView?.DisplayBlock.ParsedText is MathParsedText layout
			? layout.IndexStorageByteCount
			: 0;
}
