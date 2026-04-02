#if __SKIA__
using System;
using System.Collections.Generic;

namespace Microsoft.UI.Text
{
	/// <summary>
	/// Provides a concrete implementation of <see cref="ITextParagraphFormat"/> for the Skia rich text editing pipeline.
	/// </summary>
	/// <remarks>
	/// Diverges from WinUI: WinUI uses the native RichEdit TOM (Text Object Model) for paragraph formatting.
	/// Uno implements this as a managed data class that backs the RichEditBox document model on Skia.
	/// </remarks>
	internal partial class TextParagraphFormat : ITextParagraphFormat
	{
		private readonly List<(float position, TabAlignment align, TabLeader leader)> _tabs = new();

		public ParagraphAlignment Alignment { get; set; } = ParagraphAlignment.Left;

		public float FirstLineIndent { get; private set; }

		public FormatEffect KeepTogether { get; set; } = FormatEffect.Undefined;

		public FormatEffect KeepWithNext { get; set; } = FormatEffect.Undefined;

		public float LeftIndent { get; private set; }

		public float LineSpacing { get; private set; }

		public LineSpacingRule LineSpacingRule { get; private set; } = LineSpacingRule.Single;

		public MarkerAlignment ListAlignment { get; set; } = MarkerAlignment.Left;

		public int ListLevelIndex { get; set; }

		public int ListStart { get; set; }

		public MarkerStyle ListStyle { get; set; } = MarkerStyle.Undefined;

		public float ListTab { get; set; }

		public MarkerType ListType { get; set; } = MarkerType.None;

		public FormatEffect NoLineNumber { get; set; } = FormatEffect.Undefined;

		public FormatEffect PageBreakBefore { get; set; } = FormatEffect.Undefined;

		public float RightIndent { get; set; }

		public FormatEffect RightToLeft { get; set; } = FormatEffect.Undefined;

		public float SpaceAfter { get; set; }

		public float SpaceBefore { get; set; }

		public ParagraphStyle Style { get; set; } = ParagraphStyle.None;

		public int TabCount => _tabs.Count;

		public FormatEffect WidowControl { get; set; } = FormatEffect.Undefined;

		public void AddTab(float position, TabAlignment align, TabLeader leader)
		{
			_tabs.Add((position, align, leader));
		}

		public void ClearAllTabs()
		{
			_tabs.Clear();
		}

		public void DeleteTab(float position)
		{
			_tabs.RemoveAll(t => Math.Abs(t.position - position) < 0.01f);
		}

		public ITextParagraphFormat GetClone()
		{
			var clone = new TextParagraphFormat();
			clone.SetClone(this);
			return clone;
		}

		public void GetTab(int index, out float position, out TabAlignment align, out TabLeader leader)
		{
			if (index < 0 || index >= _tabs.Count)
			{
				position = 0;
				align = TabAlignment.Left;
				leader = TabLeader.Spaces;
				return;
			}

			var tab = _tabs[index];
			position = tab.position;
			align = tab.align;
			leader = tab.leader;
		}

		public bool IsEqual(ITextParagraphFormat format)
		{
			if (format is null)
			{
				return false;
			}

			return Alignment == format.Alignment
				&& FirstLineIndent == format.FirstLineIndent
				&& LeftIndent == format.LeftIndent
				&& RightIndent == format.RightIndent
				&& LineSpacing == format.LineSpacing
				&& LineSpacingRule == format.LineSpacingRule
				&& SpaceBefore == format.SpaceBefore
				&& SpaceAfter == format.SpaceAfter;
		}

		public void SetClone(ITextParagraphFormat format)
		{
			if (format is null)
			{
				return;
			}

			Alignment = format.Alignment;
			KeepTogether = format.KeepTogether;
			KeepWithNext = format.KeepWithNext;
			LineSpacingRule = format.LineSpacingRule;
			ListAlignment = format.ListAlignment;
			ListLevelIndex = format.ListLevelIndex;
			ListStart = format.ListStart;
			ListStyle = format.ListStyle;
			ListTab = format.ListTab;
			ListType = format.ListType;
			NoLineNumber = format.NoLineNumber;
			PageBreakBefore = format.PageBreakBefore;
			RightIndent = format.RightIndent;
			RightToLeft = format.RightToLeft;
			SpaceAfter = format.SpaceAfter;
			SpaceBefore = format.SpaceBefore;
			Style = format.Style;
			WidowControl = format.WidowControl;

			// Copy indents via SetIndents
			SetIndents(format.FirstLineIndent, format.LeftIndent, RightIndent);

			// Copy line spacing
			SetLineSpacing(format.LineSpacingRule, format.LineSpacing);

			// Copy tabs
			_tabs.Clear();
			for (int i = 0; i < format.TabCount; i++)
			{
				format.GetTab(i, out var pos, out var align, out var leader);
				_tabs.Add((pos, align, leader));
			}
		}

		public void SetIndents(float start, float left, float right)
		{
			FirstLineIndent = start;
			LeftIndent = left;
			RightIndent = right;
		}

		public void SetLineSpacing(LineSpacingRule rule, float spacing)
		{
			LineSpacingRule = rule;
			LineSpacing = spacing;
		}
	}
}
#endif
