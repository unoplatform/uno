#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Text
{
	// A concrete resolved tab stop within a paragraph's formatting.
	internal readonly struct ParagraphTab : IEquatable<ParagraphTab>
	{
		public readonly float Position;
		public readonly global::Microsoft.UI.Text.TabAlignment Alignment;
		public readonly global::Microsoft.UI.Text.TabLeader Leader;

		public ParagraphTab(float position, global::Microsoft.UI.Text.TabAlignment alignment, global::Microsoft.UI.Text.TabLeader leader)
		{
			Position = position;
			Alignment = alignment;
			Leader = leader;
		}

		public bool Equals(ParagraphTab other)
			=> Position.Equals(other.Position) && Alignment == other.Alignment && Leader == other.Leader;

		public override bool Equals(object? obj) => obj is ParagraphTab other && Equals(other);

		public override int GetHashCode() => HashCode.Combine(Position, Alignment, Leader);
	}

	// Uno-specific concrete paragraph-formatting state for one paragraph in the RichEditBox Text
	// Object Model. Unlike ITextParagraphFormat (which is tri-state and can be "undefined" over a
	// mixed range), a resolved paragraph state always holds concrete values.
	//
	// TODO Uno: A *uniform* paragraph alignment (all paragraphs sharing one Center/Right/Justify value)
	// is now projected onto the shared single-TextBlock DisplayBlock (see RichEditBox.ApplyParagraphAlignment),
	// using the same TextAlignment render path as TextBox. Per-paragraph alignment divergence, indents,
	// spacing, and lists still cannot be shown by a single TextBlock and remain a documented gap — the
	// state round-trips faithfully through the Text Object Model (get/set/clone/undo/IsEqual) regardless.
	internal sealed class ParagraphFormatState : IEquatable<ParagraphFormatState>
	{
		public global::Microsoft.UI.Text.ParagraphAlignment Alignment = global::Microsoft.UI.Text.ParagraphAlignment.Left;
		public float FirstLineIndent;
		public float LeftIndent;
		public float RightIndent;
		public float SpaceBefore;
		public float SpaceAfter;
		public global::Microsoft.UI.Text.LineSpacingRule LineSpacingRule = global::Microsoft.UI.Text.LineSpacingRule.Single;
		public float LineSpacing;

		public global::Microsoft.UI.Text.MarkerType ListType = global::Microsoft.UI.Text.MarkerType.Undefined;
		public global::Microsoft.UI.Text.MarkerStyle ListStyle = global::Microsoft.UI.Text.MarkerStyle.Undefined;
		public global::Microsoft.UI.Text.MarkerAlignment ListAlignment = global::Microsoft.UI.Text.MarkerAlignment.Undefined;
		public int ListLevelIndex;
		public int ListStart;
		public float ListTab;

		public bool KeepTogether;
		public bool KeepWithNext;
		public bool NoLineNumber;
		public bool PageBreakBefore;
		public bool RightToLeft;
		public bool WidowControl;

		public global::Microsoft.UI.Text.ParagraphStyle Style = global::Microsoft.UI.Text.ParagraphStyle.Undefined;

		public List<ParagraphTab> Tabs = new();

		public ParagraphFormatState Clone()
		{
			var clone = (ParagraphFormatState)MemberwiseClone();
			clone.Tabs = new List<ParagraphTab>(Tabs);
			return clone;
		}

		public bool Equals(ParagraphFormatState? other)
		{
			if (other is null)
			{
				return false;
			}

			if (Alignment != other.Alignment
				|| !FirstLineIndent.Equals(other.FirstLineIndent)
				|| !LeftIndent.Equals(other.LeftIndent)
				|| !RightIndent.Equals(other.RightIndent)
				|| !SpaceBefore.Equals(other.SpaceBefore)
				|| !SpaceAfter.Equals(other.SpaceAfter)
				|| LineSpacingRule != other.LineSpacingRule
				|| !LineSpacing.Equals(other.LineSpacing)
				|| ListType != other.ListType
				|| ListStyle != other.ListStyle
				|| ListAlignment != other.ListAlignment
				|| ListLevelIndex != other.ListLevelIndex
				|| ListStart != other.ListStart
				|| !ListTab.Equals(other.ListTab)
				|| KeepTogether != other.KeepTogether
				|| KeepWithNext != other.KeepWithNext
				|| NoLineNumber != other.NoLineNumber
				|| PageBreakBefore != other.PageBreakBefore
				|| RightToLeft != other.RightToLeft
				|| WidowControl != other.WidowControl
				|| Style != other.Style
				|| Tabs.Count != other.Tabs.Count)
			{
				return false;
			}

			for (var i = 0; i < Tabs.Count; i++)
			{
				if (!Tabs[i].Equals(other.Tabs[i]))
				{
					return false;
				}
			}

			return true;
		}

		public override bool Equals(object? obj) => Equals(obj as ParagraphFormatState);

		public override int GetHashCode()
		{
			var hash = new HashCode();
			hash.Add(Alignment);
			hash.Add(FirstLineIndent);
			hash.Add(LeftIndent);
			hash.Add(RightIndent);
			hash.Add(SpaceBefore);
			hash.Add(SpaceAfter);
			hash.Add(LineSpacingRule);
			hash.Add(LineSpacing);
			hash.Add(ListType);
			hash.Add(ListStyle);
			hash.Add(ListAlignment);
			hash.Add(ListLevelIndex);
			hash.Add(ListStart);
			hash.Add(ListTab);
			hash.Add(KeepTogether);
			hash.Add(KeepWithNext);
			hash.Add(NoLineNumber);
			hash.Add(PageBreakBefore);
			hash.Add(RightToLeft);
			hash.Add(WidowControl);
			hash.Add(Style);
			hash.Add(Tabs.Count);
			return hash.ToHashCode();
		}
	}

	// A contiguous span of <see cref="Length"/> characters whose paragraph(s) share the same
	// <see cref="Format"/>. Paragraph formatting is stored per-character (mirroring FormatRun) so it
	// splices in lock-step with text edits; because writes always cover whole paragraphs, every
	// character inside a paragraph carries the same state. The concatenation of all runs' lengths
	// always equals the document's plain-text length.
	internal sealed class ParagraphRun
	{
		public int Length;
		public ParagraphFormatState Format;

		public ParagraphRun(int length, ParagraphFormatState format)
		{
			Length = length;
			Format = format;
		}

		public ParagraphRun Clone() => new(Length, Format.Clone());
	}
}
