#nullable enable

using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.Windows.Apps.Test.Automation.Text;

public class TextPatternRange
{
	private readonly ITextRangeProvider? _range;

	internal TextPatternRange(ITextRangeProvider? range) => _range = range;

	public string GetText(int maxLength) => _range?.GetText(maxLength) ?? string.Empty;

	public void Select() => _range?.Select();

	public void ScrollIntoView(bool alignToTop) => _range?.ScrollIntoView(alignToTop);

	public TextPatternRange Clone() => new(_range?.Clone());
}
