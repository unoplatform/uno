#nullable enable

using Microsoft.UI.Text;

namespace Microsoft.UI.Xaml.Documents;

// Immutable paragraph layout carrier projected from RichEditBox's ParagraphFormatState onto
// Runs so UnicodeText can apply indents, spacing, and list markers during line-breaking and
// rendering. Values are in DIPs (converted from TOM points at projection time). Null on Runs
// when no RichEditBox paragraph formatting applies, ensuring zero cost for plain TextBlock/Run.
internal sealed class ParagraphLayoutInfo
{
	internal float LeftIndent { get; init; }
	internal float RightIndent { get; init; }
	internal float FirstLineIndent { get; init; }
	internal float SpaceBefore { get; init; }
	internal float SpaceAfter { get; init; }
	internal LineSpacingRule LineSpacingRule { get; init; } = LineSpacingRule.Single;
	internal float LineSpacing { get; init; }
	internal bool RightToLeft { get; init; }
	internal bool IsList { get; init; }
	internal string? MarkerText { get; init; }
	internal float ListTab { get; init; }
	internal MarkerAlignment MarkerAlignment { get; init; } = MarkerAlignment.Right;

	internal bool IsDefault =>
		LeftIndent == 0 &&
		RightIndent == 0 &&
		FirstLineIndent == 0 &&
		SpaceBefore == 0 &&
		SpaceAfter == 0 &&
		LineSpacingRule == LineSpacingRule.Single &&
		LineSpacing == 0 &&
		!RightToLeft &&
		!IsList &&
		MarkerText is null &&
		ListTab == 0;
}
