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
	// The whole surface round-trips through the paragraph run model (get/set/clone/undo) and is
	// projected by the shared Skia text layout.
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

		private void ApplyIfBound(Action<UnoTextParagraphFormat> define)
		{
			if (_apply is { } apply)
			{
				var delta = new UnoTextParagraphFormat();
				define(delta);
				apply(delta);
			}
		}

		private void ApplyAllIfBound() => _apply?.Invoke(this);

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
		internal ParagraphTabMutation TabMutation;
		internal ParagraphTab TabMutationValue;
		internal float TabMutationPosition;

		public global::Microsoft.UI.Text.ParagraphAlignment Alignment
		{
			get => AlignmentValue;
			set
			{
				ValidateEnum(value, nameof(value));
				AlignmentValue = value;
				ApplyIfBound(delta => delta.AlignmentValue = value);
			}
		}

		// FirstLineIndent and LeftIndent are read-only on the interface; they are written via SetIndents.
		public float FirstLineIndent => FirstLineIndentDefined ? FirstLineIndentValue : global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;

		public global::Microsoft.UI.Text.FormatEffect KeepTogether
		{
			get => KeepTogetherEffect;
			set
			{
				ValidateEnum(value, nameof(value));
				KeepTogetherEffect = value;
				ApplyIfBound(delta => delta.KeepTogetherEffect = value);
			}
		}

		public global::Microsoft.UI.Text.FormatEffect KeepWithNext
		{
			get => KeepWithNextEffect;
			set
			{
				ValidateEnum(value, nameof(value));
				KeepWithNextEffect = value;
				ApplyIfBound(delta => delta.KeepWithNextEffect = value);
			}
		}

		public float LeftIndent => LeftIndentDefined ? LeftIndentValue : global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;

		// LineSpacing and LineSpacingRule are read-only on the interface; they are written via SetLineSpacing.
		public float LineSpacing => LineSpacingDefined ? LineSpacingValue : global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;

		public global::Microsoft.UI.Text.LineSpacingRule LineSpacingRule => LineSpacingRuleValue;

		public global::Microsoft.UI.Text.MarkerAlignment ListAlignment
		{
			get => ListAlignmentValue;
			set
			{
				ValidateEnum(value, nameof(value));
				ListAlignmentValue = value;
				ApplyIfBound(delta => delta.ListAlignmentValue = value);
			}
		}

		public int ListLevelIndex
		{
			get => ListLevelIndexDefined ? ListLevelIndexValue : global::Microsoft.UI.Text.TextConstants.UndefinedInt32Value;
			set
			{
				if (value != global::Microsoft.UI.Text.TextConstants.UndefinedInt32Value && value < 0)
				{
					throw new ArgumentOutOfRangeException(nameof(value));
				}
				ListLevelIndexValue = value;
				ListLevelIndexDefined = value != global::Microsoft.UI.Text.TextConstants.UndefinedInt32Value;
				ApplyIfBound(delta =>
				{
					delta.ListLevelIndexValue = value;
					delta.ListLevelIndexDefined = ListLevelIndexDefined;
				});
			}
		}

		public int ListStart
		{
			get => ListStartDefined ? ListStartValue : global::Microsoft.UI.Text.TextConstants.UndefinedInt32Value;
			set
			{
				ListStartValue = value;
				ListStartDefined = value != global::Microsoft.UI.Text.TextConstants.UndefinedInt32Value;
				ApplyIfBound(delta =>
				{
					delta.ListStartValue = value;
					delta.ListStartDefined = ListStartDefined;
				});
			}
		}

		public global::Microsoft.UI.Text.MarkerStyle ListStyle
		{
			get => ListStyleValue;
			set
			{
				ValidateEnum(value, nameof(value));
				ListStyleValue = value;
				ApplyIfBound(delta => delta.ListStyleValue = value);
			}
		}

		public float ListTab
		{
			get => ListTabDefined ? ListTabValue : global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
			set
			{
				ValidateFinite(value, nameof(value));
				if (value != global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue && value < 0)
				{
					throw new ArgumentOutOfRangeException(nameof(value));
				}
				ListTabValue = value;
				ListTabDefined = value != global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
				ApplyIfBound(delta =>
				{
					delta.ListTabValue = value;
					delta.ListTabDefined = ListTabDefined;
				});
			}
		}

		public global::Microsoft.UI.Text.MarkerType ListType
		{
			get => ListTypeValue;
			set
			{
				ValidateEnum(value, nameof(value));
				ListTypeValue = value;
				ApplyIfBound(delta => delta.ListTypeValue = value);
			}
		}

		public global::Microsoft.UI.Text.FormatEffect NoLineNumber
		{
			get => NoLineNumberEffect;
			set
			{
				ValidateEnum(value, nameof(value));
				NoLineNumberEffect = value;
				ApplyIfBound(delta => delta.NoLineNumberEffect = value);
			}
		}

		public global::Microsoft.UI.Text.FormatEffect PageBreakBefore
		{
			get => PageBreakBeforeEffect;
			set
			{
				ValidateEnum(value, nameof(value));
				PageBreakBeforeEffect = value;
				ApplyIfBound(delta => delta.PageBreakBeforeEffect = value);
			}
		}

		public float RightIndent
		{
			get => RightIndentDefined ? RightIndentValue : global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
			set
			{
				ValidateFinite(value, nameof(value));
				RightIndentValue = value;
				RightIndentDefined = value != global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
				ApplyIfBound(delta =>
				{
					delta.RightIndentValue = value;
					delta.RightIndentDefined = RightIndentDefined;
				});
			}
		}

		public global::Microsoft.UI.Text.FormatEffect RightToLeft
		{
			get => RightToLeftEffect;
			set
			{
				ValidateEnum(value, nameof(value));
				RightToLeftEffect = value;
				ApplyIfBound(delta => delta.RightToLeftEffect = value);
			}
		}

		public float SpaceAfter
		{
			get => SpaceAfterDefined ? SpaceAfterValue : global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
			set
			{
				ValidateFinite(value, nameof(value));
				SpaceAfterValue = value;
				SpaceAfterDefined = value != global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
				ApplyIfBound(delta =>
				{
					delta.SpaceAfterValue = value;
					delta.SpaceAfterDefined = SpaceAfterDefined;
				});
			}
		}

		public float SpaceBefore
		{
			get => SpaceBeforeDefined ? SpaceBeforeValue : global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
			set
			{
				ValidateFinite(value, nameof(value));
				SpaceBeforeValue = value;
				SpaceBeforeDefined = value != global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
				ApplyIfBound(delta =>
				{
					delta.SpaceBeforeValue = value;
					delta.SpaceBeforeDefined = SpaceBeforeDefined;
				});
			}
		}

		public global::Microsoft.UI.Text.ParagraphStyle Style
		{
			get => StyleValue;
			set
			{
				ValidateEnum(value, nameof(value));
				StyleValue = value;
				ApplyIfBound(delta => delta.StyleValue = value);
			}
		}

		public int TabCount => TabsValue.Count;

		public global::Microsoft.UI.Text.FormatEffect WidowControl
		{
			get => WidowControlEffect;
			set
			{
				ValidateEnum(value, nameof(value));
				WidowControlEffect = value;
				ApplyIfBound(delta => delta.WidowControlEffect = value);
			}
		}

		public void AddTab(float position, global::Microsoft.UI.Text.TabAlignment align, global::Microsoft.UI.Text.TabLeader leader)
		{
			ParagraphFormatState.ValidateTab(new ParagraphTab(position, align, leader));
			if (TabsValue.Count >= ParagraphFormatState.MaxTabs && !TabsValue.Exists(tab => tab.Position.Equals(position)))
			{
				throw new ArgumentException("The paragraph contains too many tab stops.", nameof(position));
			}

			TabsValue.RemoveAll(tab => tab.Position.Equals(position));
			TabsValue.Add(new ParagraphTab(position, align, leader));
			TabsValue.Sort(static (a, b) => a.Position.CompareTo(b.Position));
			TabsDefined = true;
			ApplyIfBound(delta =>
			{
				delta.TabMutation = ParagraphTabMutation.Add;
				delta.TabMutationValue = new ParagraphTab(position, align, leader);
			});
		}

		public void ClearAllTabs()
		{
			TabsValue.Clear();
			TabsDefined = true;
			ApplyIfBound(delta =>
			{
				delta.TabMutation = ParagraphTabMutation.Clear;
			});
		}

		public void DeleteTab(float position)
		{
			if (!float.IsFinite(position) || position < 0)
			{
				throw new ArgumentException("The paragraph tab position is invalid.", nameof(position));
			}

			TabsValue.RemoveAll(t => t.Position.Equals(position));
			TabsDefined = true;
			ApplyIfBound(delta =>
			{
				delta.TabMutation = ParagraphTabMutation.Delete;
				delta.TabMutationPosition = position;
			});
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
				ApplyAllIfBound();
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
			ValidateFinite(start, nameof(start));
			ValidateFinite(left, nameof(left));
			ValidateFinite(right, nameof(right));
			FirstLineIndentValue = start;
			FirstLineIndentDefined = start != global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
			LeftIndentValue = left;
			LeftIndentDefined = left != global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
			RightIndentValue = right;
			RightIndentDefined = right != global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
			ApplyIfBound(delta =>
			{
				delta.FirstLineIndentValue = start;
				delta.FirstLineIndentDefined = start != global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
				delta.LeftIndentValue = left;
				delta.LeftIndentDefined = left != global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
				delta.RightIndentValue = right;
				delta.RightIndentDefined = right != global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
			});
		}

		public void SetLineSpacing(global::Microsoft.UI.Text.LineSpacingRule rule, float spacing)
		{
			ValidateEnum(rule, nameof(rule));
			ValidateFinite(spacing, nameof(spacing));
			if (rule == global::Microsoft.UI.Text.LineSpacingRule.Percent)
			{
				throw new ArgumentException("Percent line spacing is not supported by RichEditBox.", nameof(rule));
			}

			LineSpacingRuleValue = rule;
			LineSpacingValue = spacing;
			LineSpacingDefined = spacing != global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
			ApplyIfBound(delta =>
			{
				delta.LineSpacingRuleValue = rule;
				delta.LineSpacingValue = spacing;
				delta.LineSpacingDefined = LineSpacingDefined;
			});
		}

		private static void ValidateFinite(float value, string parameterName)
		{
			if (!float.IsFinite(value))
			{
				throw new ArgumentException("The paragraph value must be finite.", parameterName);
			}
		}

		private static void ValidateEnum<T>(T value, string parameterName) where T : struct, Enum
		{
			if (!Enum.IsDefined(value))
			{
				throw new ArgumentException("The paragraph value is not defined.", parameterName);
			}
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
			ApplyScalarsTo(state);
			ApplyTabsTo(state);
		}

		internal bool UpdatesTabs => TabMutation != ParagraphTabMutation.None || TabsDefined;

		internal void ApplyScalarsTo(ParagraphFormatState state)
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
				state.KeepTogether = ResolveEffect(KeepTogetherEffect, state.KeepTogether);
			}

			if (KeepWithNextEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.KeepWithNext = ResolveEffect(KeepWithNextEffect, state.KeepWithNext);
			}

			if (NoLineNumberEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.NoLineNumber = ResolveEffect(NoLineNumberEffect, state.NoLineNumber);
			}

			if (PageBreakBeforeEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.PageBreakBefore = ResolveEffect(PageBreakBeforeEffect, state.PageBreakBefore);
			}

			if (RightToLeftEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.RightToLeft = ResolveEffect(RightToLeftEffect, state.RightToLeft);
			}

			if (WidowControlEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.WidowControl = ResolveEffect(WidowControlEffect, state.WidowControl);
			}

			if (StyleValue != global::Microsoft.UI.Text.ParagraphStyle.Undefined)
			{
				state.Style = StyleValue;
			}

		}

		internal void ApplyTabsTo(ParagraphFormatState state)
		{
			switch (TabMutation)
			{
				case ParagraphTabMutation.Add:
					var addedTabs = new List<ParagraphTab>(state.Tabs);
					addedTabs.RemoveAll(tab => tab.Position.Equals(TabMutationValue.Position));
					addedTabs.Add(TabMutationValue);
					addedTabs.Sort(static (left, right) => left.Position.CompareTo(right.Position));
					state.SetTabs(addedTabs);
					break;
				case ParagraphTabMutation.Delete:
					var remainingTabs = new List<ParagraphTab>(state.Tabs);
					remainingTabs.RemoveAll(tab => tab.Position.Equals(TabMutationPosition));
					state.SetTabs(remainingTabs);
					break;
				case ParagraphTabMutation.Clear:
					state.SetTabs(Array.Empty<ParagraphTab>());
					break;
				case ParagraphTabMutation.None when TabsDefined:
					state.SetTabs(TabsValue);
					break;
			}
		}

		internal enum ParagraphTabMutation
		{
			None,
			Add,
			Delete,
			Clear,
		}

		private static global::Microsoft.UI.Text.FormatEffect Effect(bool value)
			=> value ? global::Microsoft.UI.Text.FormatEffect.On : global::Microsoft.UI.Text.FormatEffect.Off;

		// Resolves a tri-state FormatEffect against the current paragraph boolean: On/Off set the value
		// directly, Toggle flips it (WinUI's tomToggle), Undefined leaves it unchanged. Applied per
		// paragraph (via ApplyParagraphFormatOverParagraphs), so a Toggle flips each paragraph independently.
		private static bool ResolveEffect(global::Microsoft.UI.Text.FormatEffect effect, bool current)
			=> effect switch
			{
				global::Microsoft.UI.Text.FormatEffect.On => true,
				global::Microsoft.UI.Text.FormatEffect.Off => false,
				global::Microsoft.UI.Text.FormatEffect.Toggle => !current,
				_ => current,
			};
	}
}
