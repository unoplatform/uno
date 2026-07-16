#nullable enable

using System;

namespace Microsoft.UI.Text
{
	// Uno-specific concrete character-formatting state for one contiguous run of text in the
	// RichEditBox Text Object Model. Unlike ITextCharacterFormat (which is tri-state and can be
	// "undefined" over a mixed range), a run always holds concrete, resolved values.
	//
	// Every ITextCharacterFormat property is persisted. The RichEditBox renderer projects the subset
	// with exact shared-layout equivalents; the remaining values are retained for TOM fidelity.
	internal sealed class CharacterFormatState : IEquatable<CharacterFormatState>
	{
		internal const int MaxLanguageTagLength = 256;

		public bool AllCaps;
		public global::Windows.UI.Color? Background;
		public bool Bold;
		public global::Windows.UI.Text.FontStretch FontStretch = global::Windows.UI.Text.FontStretch.Normal;
		public bool Hidden;
		public bool Italic;
		public float Kerning;
		public string LanguageTag = string.Empty;
		public bool Outline;
		public float Position;
		public bool ProtectedText;
		public bool SmallCaps;
		public float Spacing;
		public bool Subscript;
		public bool Superscript;
		public global::Microsoft.UI.Text.UnderlineType Underline = global::Microsoft.UI.Text.UnderlineType.None;
		public bool Strikethrough;
		public global::Microsoft.UI.Text.TextScript TextScript = global::Microsoft.UI.Text.TextScript.Default;
		public global::Windows.UI.Color? Foreground;
		public int Weight = 400;
		public bool WeightExplicit;

		/// <summary>Font size in pixels; 0 means "inherit from the control".</summary>
		public float Size;

		/// <summary>Font family name; null or empty means "inherit from the control".</summary>
		public string? Name;

		/// <summary>Quoted TOM URL text for a client-created friendly-name link; null means not a link.</summary>
		public string? Link;

		/// <summary>Inline object occupying this character position; null for ordinary text.</summary>
		public InlineImageState? InlineImage;

		public CharacterFormatState Clone()
		{
			var clone = (CharacterFormatState)MemberwiseClone();
			clone.InlineImage = InlineImage?.Clone();
			return clone;
		}

		public bool Equals(CharacterFormatState? other)
			=> other is not null
				&& AllCaps == other.AllCaps
				&& Nullable.Equals(Background, other.Background)
				&& Bold == other.Bold
				&& FontStretch == other.FontStretch
				&& Hidden == other.Hidden
				&& Italic == other.Italic
				&& Kerning.Equals(other.Kerning)
				&& string.Equals(LanguageTag, other.LanguageTag, StringComparison.Ordinal)
				&& Outline == other.Outline
				&& Position.Equals(other.Position)
				&& ProtectedText == other.ProtectedText
				&& SmallCaps == other.SmallCaps
				&& Spacing.Equals(other.Spacing)
				&& Subscript == other.Subscript
				&& Superscript == other.Superscript
				&& Underline == other.Underline
				&& Strikethrough == other.Strikethrough
				&& TextScript == other.TextScript
				&& Nullable.Equals(Foreground, other.Foreground)
				&& Weight == other.Weight
				&& WeightExplicit == other.WeightExplicit
				&& Size.Equals(other.Size)
				&& string.Equals(Name, other.Name, StringComparison.Ordinal)
				&& string.Equals(Link, other.Link, StringComparison.Ordinal)
				&& Equals(InlineImage, other.InlineImage);

		public override bool Equals(object? obj) => Equals(obj as CharacterFormatState);

		public override int GetHashCode()
		{
			var hash = new HashCode();
			hash.Add(AllCaps);
			hash.Add(Background);
			hash.Add(Bold);
			hash.Add(FontStretch);
			hash.Add(Hidden);
			hash.Add(Italic);
			hash.Add(Kerning);
			hash.Add(LanguageTag, StringComparer.Ordinal);
			hash.Add(Outline);
			hash.Add(Position);
			hash.Add(ProtectedText);
			hash.Add(SmallCaps);
			hash.Add(Spacing);
			hash.Add(Subscript);
			hash.Add(Superscript);
			hash.Add(Underline);
			hash.Add(Strikethrough);
			hash.Add(TextScript);
			hash.Add(Foreground);
			hash.Add(Weight);
			hash.Add(WeightExplicit);
			hash.Add(Size);
			hash.Add(Name, StringComparer.Ordinal);
			hash.Add(Link, StringComparer.Ordinal);
			hash.Add(InlineImage);
			return hash.ToHashCode();
		}
	}

	// A contiguous span of <see cref="Length"/> characters sharing the same <see cref="Format"/>.
	// The concatenation of all runs' lengths always equals the document's plain-text length.
	internal sealed class FormatRun
	{
		public int Length;
		public CharacterFormatState Format;

		public FormatRun(int length, CharacterFormatState format)
		{
			Length = length;
			Format = format;
		}

		public FormatRun Clone() => new(Length, Format.Clone());
	}
}
