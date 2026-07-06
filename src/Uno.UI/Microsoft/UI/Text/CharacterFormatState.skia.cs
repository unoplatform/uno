#nullable enable

using System;

namespace Microsoft.UI.Text
{
	// Uno-specific concrete character-formatting state for one contiguous run of text in the
	// RichEditBox Text Object Model. Unlike ITextCharacterFormat (which is tri-state and can be
	// "undefined" over a mixed range), a run always holds concrete, resolved values.
	//
	// The tracked subset is exactly what the plain-text engine can render as TextBlock inlines:
	// bold, italic, underline, strikethrough, foreground color, font size and font family.
	// The remaining ITextCharacterFormat surface (AllCaps, Hidden, Kerning, sub/superscript, etc.)
	// is exposed on UnoTextCharacterFormat but not yet persisted per-run — see the TODO Uno notes there.
	internal sealed class CharacterFormatState : IEquatable<CharacterFormatState>
	{
		public bool Bold;
		public bool Italic;
		public global::Microsoft.UI.Text.UnderlineType Underline = global::Microsoft.UI.Text.UnderlineType.None;
		public bool Strikethrough;
		public global::Windows.UI.Color? Foreground;

		/// <summary>Font size in pixels; 0 means "inherit from the control".</summary>
		public float Size;

		/// <summary>Font family name; null or empty means "inherit from the control".</summary>
		public string? Name;

		public CharacterFormatState Clone() => (CharacterFormatState)MemberwiseClone();

		public bool Equals(CharacterFormatState? other)
			=> other is not null
				&& Bold == other.Bold
				&& Italic == other.Italic
				&& Underline == other.Underline
				&& Strikethrough == other.Strikethrough
				&& Nullable.Equals(Foreground, other.Foreground)
				&& Size.Equals(other.Size)
				&& string.Equals(Name, other.Name, StringComparison.Ordinal);

		public override bool Equals(object? obj) => Equals(obj as CharacterFormatState);

		public override int GetHashCode()
		{
			var hash = new HashCode();
			hash.Add(Bold);
			hash.Add(Italic);
			hash.Add(Underline);
			hash.Add(Strikethrough);
			hash.Add(Foreground);
			hash.Add(Size);
			hash.Add(Name, StringComparer.Ordinal);
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
