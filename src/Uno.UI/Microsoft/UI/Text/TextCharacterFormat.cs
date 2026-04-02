#if __SKIA__
using System;
using Windows.UI;

namespace Microsoft.UI.Text
{
	/// <summary>
	/// Provides a concrete implementation of <see cref="ITextCharacterFormat"/> for the Skia rich text editing pipeline.
	/// </summary>
	/// <remarks>
	/// Diverges from WinUI: WinUI uses the native RichEdit TOM (Text Object Model) for character formatting.
	/// Uno implements this as a managed data class that backs the RichEditBox document model on Skia.
	/// </remarks>
	internal partial class TextCharacterFormat : ITextCharacterFormat
	{
		public FormatEffect AllCaps { get; set; } = FormatEffect.Undefined;

		public Color BackgroundColor { get; set; } = Colors.Transparent;

		public FormatEffect Bold { get; set; } = FormatEffect.Undefined;

		public global::Windows.UI.Text.FontStretch FontStretch { get; set; } = global::Windows.UI.Text.FontStretch.Undefined;

		public global::Windows.UI.Text.FontStyle FontStyle { get; set; } = global::Windows.UI.Text.FontStyle.Normal;

		public Color ForegroundColor { get; set; } = Colors.Black;

		public FormatEffect Hidden { get; set; } = FormatEffect.Undefined;

		public FormatEffect Italic { get; set; } = FormatEffect.Undefined;

		public float Kerning { get; set; }

		public string LanguageTag { get; set; } = string.Empty;

		public LinkType LinkType => LinkType.Undefined;

		public string Name { get; set; } = string.Empty;

		public FormatEffect Outline { get; set; } = FormatEffect.Undefined;

		public float Position { get; set; }

		public FormatEffect ProtectedText { get; set; } = FormatEffect.Undefined;

		public float Size { get; set; } = 0f;

		public FormatEffect SmallCaps { get; set; } = FormatEffect.Undefined;

		public float Spacing { get; set; }

		public FormatEffect Strikethrough { get; set; } = FormatEffect.Undefined;

		public FormatEffect Subscript { get; set; } = FormatEffect.Undefined;

		public FormatEffect Superscript { get; set; } = FormatEffect.Undefined;

		public TextScript TextScript { get; set; } = TextScript.Undefined;

		public UnderlineType Underline { get; set; } = UnderlineType.Undefined;

		public int Weight { get; set; } = 0;

		public ITextCharacterFormat GetClone()
		{
			var clone = new TextCharacterFormat();
			clone.SetClone(this);
			return clone;
		}

		public void SetClone(ITextCharacterFormat value)
		{
			if (value is null)
			{
				return;
			}

			AllCaps = value.AllCaps;
			BackgroundColor = value.BackgroundColor;
			Bold = value.Bold;
			FontStretch = value.FontStretch;
			FontStyle = value.FontStyle;
			ForegroundColor = value.ForegroundColor;
			Hidden = value.Hidden;
			Italic = value.Italic;
			Kerning = value.Kerning;
			LanguageTag = value.LanguageTag;
			Name = value.Name;
			Outline = value.Outline;
			Position = value.Position;
			ProtectedText = value.ProtectedText;
			Size = value.Size;
			SmallCaps = value.SmallCaps;
			Spacing = value.Spacing;
			Strikethrough = value.Strikethrough;
			Subscript = value.Subscript;
			Superscript = value.Superscript;
			TextScript = value.TextScript;
			Underline = value.Underline;
			Weight = value.Weight;
		}

		public bool IsEqual(ITextCharacterFormat format)
		{
			if (format is null)
			{
				return false;
			}

			return AllCaps == format.AllCaps
				&& BackgroundColor == format.BackgroundColor
				&& Bold == format.Bold
				&& FontStretch == format.FontStretch
				&& FontStyle == format.FontStyle
				&& ForegroundColor == format.ForegroundColor
				&& Hidden == format.Hidden
				&& Italic == format.Italic
				&& Kerning == format.Kerning
				&& LanguageTag == format.LanguageTag
				&& Name == format.Name
				&& Outline == format.Outline
				&& Position == format.Position
				&& ProtectedText == format.ProtectedText
				&& Size == format.Size
				&& SmallCaps == format.SmallCaps
				&& Spacing == format.Spacing
				&& Strikethrough == format.Strikethrough
				&& Subscript == format.Subscript
				&& Superscript == format.Superscript
				&& TextScript == format.TextScript
				&& Underline == format.Underline
				&& Weight == format.Weight;
		}

		/// <summary>
		/// Merges formatting from another format, setting properties to Undefined where they differ.
		/// Used when computing the effective format for a multi-character range with mixed formatting.
		/// </summary>
		internal void MergeWith(ITextCharacterFormat other)
		{
			if (Bold != other.Bold) { Bold = FormatEffect.Undefined; }
			if (Italic != other.Italic) { Italic = FormatEffect.Undefined; }
			if (Underline != other.Underline) { Underline = UnderlineType.Undefined; }
			if (Strikethrough != other.Strikethrough) { Strikethrough = FormatEffect.Undefined; }
			if (Size != other.Size) { Size = 0f; }
			if (Name != other.Name) { Name = string.Empty; }
			if (Weight != other.Weight) { Weight = 0; }
			if (ForegroundColor != other.ForegroundColor) { ForegroundColor = default; }
			if (BackgroundColor != other.BackgroundColor) { BackgroundColor = default; }
			if (FontStyle != other.FontStyle) { FontStyle = global::Windows.UI.Text.FontStyle.Normal; }
			if (AllCaps != other.AllCaps) { AllCaps = FormatEffect.Undefined; }
			if (SmallCaps != other.SmallCaps) { SmallCaps = FormatEffect.Undefined; }
			if (Subscript != other.Subscript) { Subscript = FormatEffect.Undefined; }
			if (Superscript != other.Superscript) { Superscript = FormatEffect.Undefined; }
			if (Hidden != other.Hidden) { Hidden = FormatEffect.Undefined; }
			if (Outline != other.Outline) { Outline = FormatEffect.Undefined; }
			if (ProtectedText != other.ProtectedText) { ProtectedText = FormatEffect.Undefined; }
		}
	}
}
#endif
