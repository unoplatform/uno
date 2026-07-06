#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Text
{
	// Uno-specific functional implementation of ITextParagraphFormat for the RichEditBox Text Object
	// Model. This is a tri-state value object: each property can be a concrete value or "undefined"
	// (ParagraphAlignment.Undefined / FormatEffect.Undefined / an unset "defined" flag for numeric
	// values) when it is read over a range whose paragraphs disagree. Applying a format only writes
	// back the properties that are defined.
	//
	// TODO Uno: The whole surface round-trips through the paragraph run model (get/set/clone/undo)
	// but nothing is rendered yet — the shared DisplayBlock is a single TextBlock and cannot show
	// per-paragraph alignment/indents/spacing/lists. Visual rendering is a documented gap.
	internal sealed class UnoTextParagraphFormat : global::Microsoft.UI.Text.ITextParagraphFormat
	{
		// When bound, each setter applies immediately through the bound callback: for a range-bound
		// format (via the range's ParagraphFormat getter) this pushes into that range's paragraphs —
		// making the canonical `range.ParagraphFormat.Alignment = Center` idiom work, matching WinUI's
		// live paragraph-format object; for the document default (GetDefaultParagraphFormat) it writes
		// the document's default state. A cloned or default-constructed format is unbound and behaves as
		// a plain value object.
		private Action<UnoTextParagraphFormat>? _apply;

		internal void Bind(UnoTextRange range) => _apply = range.ApplyParagraphFormat;

		internal void BindApply(Action<UnoTextParagraphFormat> apply) => _apply = apply;

		private void ApplyIfBound() => _apply?.Invoke(this);

		// Enum-typed properties use their Undefined member as the "not defined" marker; FormatEffect
		// properties use FormatEffect.Undefined; numeric properties carry an explicit "defined" flag.
		internal global::Microsoft.UI.Text.ParagraphAlignment AlignmentValue = global::Microsoft.UI.Text.ParagraphAlignment.Undefined;
		internal bool FirstLineIndentDefined;
		internal float FirstLineIndentValue;
		internal bool LeftIndentDefined;
		internal float LeftIndentValue;
		internal bool RightIndentDefined;
		internal float RightIndentValue;
		internal bool SpaceBeforeDefined;
		internal float SpaceBeforeValue;
		internal bool SpaceAfterDefined;
		internal float SpaceAfterValue;
		internal global::Microsoft.UI.Text.LineSpacingRule LineSpacingRuleValue = global::Microsoft.UI.Text.LineSpacingRule.Undefined;
		internal bool LineSpacingDefined;
		internal float LineSpacingValue;
		internal global::Microsoft.UI.Text.MarkerType ListTypeValue = global::Microsoft.UI.Text.MarkerType.Undefined;
		internal global::Microsoft.UI.Text.MarkerStyle ListStyleValue = global::Microsoft.UI.Text.MarkerStyle.Undefined;
		internal global::Microsoft.UI.Text.MarkerAlignment ListAlignmentValue = global::Microsoft.UI.Text.MarkerAlignment.Undefined;
		internal bool ListLevelIndexDefined;
		internal int ListLevelIndexValue;
		internal bool ListStartDefined;
		internal int ListStartValue;
		internal bool ListTabDefined;
		internal float ListTabValue;
		internal global::Microsoft.UI.Text.FormatEffect KeepTogetherEffect = global::Microsoft.UI.Text.FormatEffect.Undefined;
		internal global::Microsoft.UI.Text.FormatEffect KeepWithNextEffect = global::Microsoft.UI.Text.FormatEffect.Undefined;
		internal global::Microsoft.UI.Text.FormatEffect NoLineNumberEffect = global::Microsoft.UI.Text.FormatEffect.Undefined;
		internal global::Microsoft.UI.Text.FormatEffect PageBreakBeforeEffect = global::Microsoft.UI.Text.FormatEffect.Undefined;
		internal global::Microsoft.UI.Text.FormatEffect RightToLeftEffect = global::Microsoft.UI.Text.FormatEffect.Undefined;
		internal global::Microsoft.UI.Text.FormatEffect WidowControlEffect = global::Microsoft.UI.Text.FormatEffect.Undefined;
		internal global::Microsoft.UI.Text.ParagraphStyle StyleValue = global::Microsoft.UI.Text.ParagraphStyle.Undefined;
		internal bool TabsDefined;
		internal List<ParagraphTab> TabsValue = new();

		public global::Microsoft.UI.Text.ParagraphAlignment Alignment
		{
			get => AlignmentValue;
			set
			{
				AlignmentValue = value;
				ApplyIfBound();
			}
		}

		// FirstLineIndent and LeftIndent are read-only on the interface; they are written via SetIndents.
		public float FirstLineIndent => FirstLineIndentDefined ? FirstLineIndentValue : 0f;

		public global::Microsoft.UI.Text.FormatEffect KeepTogether
		{
			get => KeepTogetherEffect;
			set
			{
				KeepTogetherEffect = value;
				ApplyIfBound();
			}
		}

		public global::Microsoft.UI.Text.FormatEffect KeepWithNext
		{
			get => KeepWithNextEffect;
			set
			{
				KeepWithNextEffect = value;
				ApplyIfBound();
			}
		}

		public float LeftIndent => LeftIndentDefined ? LeftIndentValue : 0f;

		// LineSpacing and LineSpacingRule are read-only on the interface; they are written via SetLineSpacing.
		public float LineSpacing => LineSpacingDefined ? LineSpacingValue : 0f;

		public global::Microsoft.UI.Text.LineSpacingRule LineSpacingRule => LineSpacingRuleValue;

		public global::Microsoft.UI.Text.MarkerAlignment ListAlignment
		{
			get => ListAlignmentValue;
			set
			{
				ListAlignmentValue = value;
				ApplyIfBound();
			}
		}

		public int ListLevelIndex
		{
			get => ListLevelIndexDefined ? ListLevelIndexValue : 0;
			set
			{
				ListLevelIndexValue = value;
				ListLevelIndexDefined = true;
				ApplyIfBound();
			}
		}

		public int ListStart
		{
			get => ListStartDefined ? ListStartValue : 0;
			set
			{
				ListStartValue = value;
				ListStartDefined = true;
				ApplyIfBound();
			}
		}

		public global::Microsoft.UI.Text.MarkerStyle ListStyle
		{
			get => ListStyleValue;
			set
			{
				ListStyleValue = value;
				ApplyIfBound();
			}
		}

		public float ListTab
		{
			get => ListTabDefined ? ListTabValue : 0f;
			set
			{
				ListTabValue = value;
				ListTabDefined = true;
				ApplyIfBound();
			}
		}

		public global::Microsoft.UI.Text.MarkerType ListType
		{
			get => ListTypeValue;
			set
			{
				ListTypeValue = value;
				ApplyIfBound();
			}
		}

		public global::Microsoft.UI.Text.FormatEffect NoLineNumber
		{
			get => NoLineNumberEffect;
			set
			{
				NoLineNumberEffect = value;
				ApplyIfBound();
			}
		}

		public global::Microsoft.UI.Text.FormatEffect PageBreakBefore
		{
			get => PageBreakBeforeEffect;
			set
			{
				PageBreakBeforeEffect = value;
				ApplyIfBound();
			}
		}

		public float RightIndent
		{
			get => RightIndentDefined ? RightIndentValue : 0f;
			set
			{
				RightIndentValue = value;
				RightIndentDefined = true;
				ApplyIfBound();
			}
		}

		public global::Microsoft.UI.Text.FormatEffect RightToLeft
		{
			get => RightToLeftEffect;
			set
			{
				RightToLeftEffect = value;
				ApplyIfBound();
			}
		}

		public float SpaceAfter
		{
			get => SpaceAfterDefined ? SpaceAfterValue : 0f;
			set
			{
				SpaceAfterValue = value;
				SpaceAfterDefined = true;
				ApplyIfBound();
			}
		}

		public float SpaceBefore
		{
			get => SpaceBeforeDefined ? SpaceBeforeValue : 0f;
			set
			{
				SpaceBeforeValue = value;
				SpaceBeforeDefined = true;
				ApplyIfBound();
			}
		}

		public global::Microsoft.UI.Text.ParagraphStyle Style
		{
			get => StyleValue;
			set
			{
				StyleValue = value;
				ApplyIfBound();
			}
		}

		public int TabCount => TabsValue.Count;

		public global::Microsoft.UI.Text.FormatEffect WidowControl
		{
			get => WidowControlEffect;
			set
			{
				WidowControlEffect = value;
				ApplyIfBound();
			}
		}

		public void AddTab(float position, global::Microsoft.UI.Text.TabAlignment align, global::Microsoft.UI.Text.TabLeader leader)
		{
			TabsValue.Add(new ParagraphTab(position, align, leader));
			TabsValue.Sort(static (a, b) => a.Position.CompareTo(b.Position));
			TabsDefined = true;
			ApplyIfBound();
		}

		public void ClearAllTabs()
		{
			TabsValue.Clear();
			TabsDefined = true;
			ApplyIfBound();
		}

		public void DeleteTab(float position)
		{
			TabsValue.RemoveAll(t => t.Position.Equals(position));
			TabsDefined = true;
			ApplyIfBound();
		}

		public void GetTab(int index, out float position, out global::Microsoft.UI.Text.TabAlignment align, out global::Microsoft.UI.Text.TabLeader leader)
		{
			// TODO Uno: WinUI's GetTab also accepts special negative indices (tomTabBack/Next/Here);
			// only direct 0-based indexing is supported here.
			if (index >= 0 && index < TabsValue.Count)
			{
				var tab = TabsValue[index];
				position = tab.Position;
				align = tab.Alignment;
				leader = tab.Leader;
				return;
			}

			position = 0f;
			align = global::Microsoft.UI.Text.TabAlignment.Left;
			leader = global::Microsoft.UI.Text.TabLeader.Spaces;
		}

		public global::Microsoft.UI.Text.ITextParagraphFormat GetClone()
		{
			var clone = new UnoTextParagraphFormat();
			clone.CopyFrom(this);
			return clone;
		}

		public void SetClone(global::Microsoft.UI.Text.ITextParagraphFormat format)
		{
			if (format is UnoTextParagraphFormat other)
			{
				CopyFrom(other);
			}
		}

		public bool IsEqual(global::Microsoft.UI.Text.ITextParagraphFormat format)
			=> format is UnoTextParagraphFormat other
				&& AlignmentValue == other.AlignmentValue
				&& FirstLineIndentDefined == other.FirstLineIndentDefined
				&& FirstLineIndentValue.Equals(other.FirstLineIndentValue)
				&& LeftIndentDefined == other.LeftIndentDefined
				&& LeftIndentValue.Equals(other.LeftIndentValue)
				&& RightIndentDefined == other.RightIndentDefined
				&& RightIndentValue.Equals(other.RightIndentValue)
				&& SpaceBeforeDefined == other.SpaceBeforeDefined
				&& SpaceBeforeValue.Equals(other.SpaceBeforeValue)
				&& SpaceAfterDefined == other.SpaceAfterDefined
				&& SpaceAfterValue.Equals(other.SpaceAfterValue)
				&& LineSpacingRuleValue == other.LineSpacingRuleValue
				&& LineSpacingDefined == other.LineSpacingDefined
				&& LineSpacingValue.Equals(other.LineSpacingValue)
				&& ListTypeValue == other.ListTypeValue
				&& ListStyleValue == other.ListStyleValue
				&& ListAlignmentValue == other.ListAlignmentValue
				&& ListLevelIndexDefined == other.ListLevelIndexDefined
				&& ListLevelIndexValue == other.ListLevelIndexValue
				&& ListStartDefined == other.ListStartDefined
				&& ListStartValue == other.ListStartValue
				&& ListTabDefined == other.ListTabDefined
				&& ListTabValue.Equals(other.ListTabValue)
				&& KeepTogetherEffect == other.KeepTogetherEffect
				&& KeepWithNextEffect == other.KeepWithNextEffect
				&& NoLineNumberEffect == other.NoLineNumberEffect
				&& PageBreakBeforeEffect == other.PageBreakBeforeEffect
				&& RightToLeftEffect == other.RightToLeftEffect
				&& WidowControlEffect == other.WidowControlEffect
				&& StyleValue == other.StyleValue
				&& TabsDefined == other.TabsDefined
				&& RichEditTextDocument.TabsEqual(TabsValue, other.TabsValue);

		public void SetIndents(float start, float left, float right)
		{
			FirstLineIndentValue = start;
			FirstLineIndentDefined = true;
			LeftIndentValue = left;
			LeftIndentDefined = true;
			RightIndentValue = right;
			RightIndentDefined = true;
			ApplyIfBound();
		}

		public void SetLineSpacing(global::Microsoft.UI.Text.LineSpacingRule rule, float spacing)
		{
			LineSpacingRuleValue = rule;
			LineSpacingValue = spacing;
			LineSpacingDefined = true;
			ApplyIfBound();
		}

		private void CopyFrom(UnoTextParagraphFormat other)
		{
			AlignmentValue = other.AlignmentValue;
			FirstLineIndentDefined = other.FirstLineIndentDefined;
			FirstLineIndentValue = other.FirstLineIndentValue;
			LeftIndentDefined = other.LeftIndentDefined;
			LeftIndentValue = other.LeftIndentValue;
			RightIndentDefined = other.RightIndentDefined;
			RightIndentValue = other.RightIndentValue;
			SpaceBeforeDefined = other.SpaceBeforeDefined;
			SpaceBeforeValue = other.SpaceBeforeValue;
			SpaceAfterDefined = other.SpaceAfterDefined;
			SpaceAfterValue = other.SpaceAfterValue;
			LineSpacingRuleValue = other.LineSpacingRuleValue;
			LineSpacingDefined = other.LineSpacingDefined;
			LineSpacingValue = other.LineSpacingValue;
			ListTypeValue = other.ListTypeValue;
			ListStyleValue = other.ListStyleValue;
			ListAlignmentValue = other.ListAlignmentValue;
			ListLevelIndexDefined = other.ListLevelIndexDefined;
			ListLevelIndexValue = other.ListLevelIndexValue;
			ListStartDefined = other.ListStartDefined;
			ListStartValue = other.ListStartValue;
			ListTabDefined = other.ListTabDefined;
			ListTabValue = other.ListTabValue;
			KeepTogetherEffect = other.KeepTogetherEffect;
			KeepWithNextEffect = other.KeepWithNextEffect;
			NoLineNumberEffect = other.NoLineNumberEffect;
			PageBreakBeforeEffect = other.PageBreakBeforeEffect;
			RightToLeftEffect = other.RightToLeftEffect;
			WidowControlEffect = other.WidowControlEffect;
			StyleValue = other.StyleValue;
			TabsDefined = other.TabsDefined;
			TabsValue = new List<ParagraphTab>(other.TabsValue);
		}

		/// <summary>Populates every property (all defined) from a single resolved paragraph state.</summary>
		internal void LoadFrom(ParagraphFormatState state)
		{
			AlignmentValue = state.Alignment;
			FirstLineIndentValue = state.FirstLineIndent;
			FirstLineIndentDefined = true;
			LeftIndentValue = state.LeftIndent;
			LeftIndentDefined = true;
			RightIndentValue = state.RightIndent;
			RightIndentDefined = true;
			SpaceBeforeValue = state.SpaceBefore;
			SpaceBeforeDefined = true;
			SpaceAfterValue = state.SpaceAfter;
			SpaceAfterDefined = true;
			LineSpacingRuleValue = state.LineSpacingRule;
			LineSpacingValue = state.LineSpacing;
			LineSpacingDefined = true;
			ListTypeValue = state.ListType;
			ListStyleValue = state.ListStyle;
			ListAlignmentValue = state.ListAlignment;
			ListLevelIndexValue = state.ListLevelIndex;
			ListLevelIndexDefined = true;
			ListStartValue = state.ListStart;
			ListStartDefined = true;
			ListTabValue = state.ListTab;
			ListTabDefined = true;
			KeepTogetherEffect = Effect(state.KeepTogether);
			KeepWithNextEffect = Effect(state.KeepWithNext);
			NoLineNumberEffect = Effect(state.NoLineNumber);
			PageBreakBeforeEffect = Effect(state.PageBreakBefore);
			RightToLeftEffect = Effect(state.RightToLeft);
			WidowControlEffect = Effect(state.WidowControl);
			StyleValue = state.Style;
			TabsValue = new List<ParagraphTab>(state.Tabs);
			TabsDefined = true;
		}

		/// <summary>Writes this format's defined properties onto a concrete paragraph state.</summary>
		internal void ApplyTo(ParagraphFormatState state)
		{
			if (AlignmentValue != global::Microsoft.UI.Text.ParagraphAlignment.Undefined)
			{
				state.Alignment = AlignmentValue;
			}

			if (FirstLineIndentDefined)
			{
				state.FirstLineIndent = FirstLineIndentValue;
			}

			if (LeftIndentDefined)
			{
				state.LeftIndent = LeftIndentValue;
			}

			if (RightIndentDefined)
			{
				state.RightIndent = RightIndentValue;
			}

			if (SpaceBeforeDefined)
			{
				state.SpaceBefore = SpaceBeforeValue;
			}

			if (SpaceAfterDefined)
			{
				state.SpaceAfter = SpaceAfterValue;
			}

			if (LineSpacingRuleValue != global::Microsoft.UI.Text.LineSpacingRule.Undefined)
			{
				state.LineSpacingRule = LineSpacingRuleValue;
			}

			if (LineSpacingDefined)
			{
				state.LineSpacing = LineSpacingValue;
			}

			if (ListTypeValue != global::Microsoft.UI.Text.MarkerType.Undefined)
			{
				state.ListType = ListTypeValue;
			}

			if (ListStyleValue != global::Microsoft.UI.Text.MarkerStyle.Undefined)
			{
				state.ListStyle = ListStyleValue;
			}

			if (ListAlignmentValue != global::Microsoft.UI.Text.MarkerAlignment.Undefined)
			{
				state.ListAlignment = ListAlignmentValue;
			}

			if (ListLevelIndexDefined)
			{
				state.ListLevelIndex = ListLevelIndexValue;
			}

			if (ListStartDefined)
			{
				state.ListStart = ListStartValue;
			}

			if (ListTabDefined)
			{
				state.ListTab = ListTabValue;
			}

			if (KeepTogetherEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.KeepTogether = KeepTogetherEffect == global::Microsoft.UI.Text.FormatEffect.On;
			}

			if (KeepWithNextEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.KeepWithNext = KeepWithNextEffect == global::Microsoft.UI.Text.FormatEffect.On;
			}

			if (NoLineNumberEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.NoLineNumber = NoLineNumberEffect == global::Microsoft.UI.Text.FormatEffect.On;
			}

			if (PageBreakBeforeEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.PageBreakBefore = PageBreakBeforeEffect == global::Microsoft.UI.Text.FormatEffect.On;
			}

			if (RightToLeftEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.RightToLeft = RightToLeftEffect == global::Microsoft.UI.Text.FormatEffect.On;
			}

			if (WidowControlEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.WidowControl = WidowControlEffect == global::Microsoft.UI.Text.FormatEffect.On;
			}

			if (StyleValue != global::Microsoft.UI.Text.ParagraphStyle.Undefined)
			{
				state.Style = StyleValue;
			}

			if (TabsDefined)
			{
				state.Tabs = new List<ParagraphTab>(TabsValue);
			}
		}

		private static global::Microsoft.UI.Text.FormatEffect Effect(bool value)
			=> value ? global::Microsoft.UI.Text.FormatEffect.On : global::Microsoft.UI.Text.FormatEffect.Off;
	}
}
