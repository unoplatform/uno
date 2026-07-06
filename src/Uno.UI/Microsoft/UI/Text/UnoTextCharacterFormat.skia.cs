#nullable enable

using System;

namespace Microsoft.UI.Text
{
	// Uno-specific functional implementation of ITextCharacterFormat for the RichEditBox Text Object
	// Model. This is a tri-state value object: each property can be a concrete value or "undefined"
	// (FormatEffect.Undefined / UnderlineType.Undefined / an unset flag) when it is read over a range
	// whose characters disagree. Applying a format only writes back the properties that are defined.
	//
	// Tracked + persisted to the run model (and rendered as TextBlock inlines in a later increment):
	// Bold, Italic, Underline, Strikethrough, ForegroundColor, Size, Name, and Weight/FontStyle
	// (aliased onto Bold/Italic). The remaining members round-trip on this object but are not yet
	// persisted per run or rendered — see the TODO Uno note below.
	internal sealed class UnoTextCharacterFormat : global::Microsoft.UI.Text.ITextCharacterFormat
	{
		// When bound to a range (via the range's CharacterFormat getter), each tracked-property setter
		// applies immediately to that range — this makes the canonical `range.CharacterFormat.Bold = On`
		// idiom work, matching WinUI's live character-format object. A cloned or default-constructed
		// format is unbound and behaves as a plain value object.
		private UnoTextRange? _boundRange;

		internal void Bind(UnoTextRange range) => _boundRange = range;

		private void ApplyIfBound() => _boundRange?.ApplyCharacterFormat(this);

		// Tracked subset (persisted to the run model).
		internal global::Microsoft.UI.Text.FormatEffect BoldEffect = global::Microsoft.UI.Text.FormatEffect.Undefined;
		internal global::Microsoft.UI.Text.FormatEffect ItalicEffect = global::Microsoft.UI.Text.FormatEffect.Undefined;
		internal global::Microsoft.UI.Text.FormatEffect StrikethroughEffect = global::Microsoft.UI.Text.FormatEffect.Undefined;
		internal global::Microsoft.UI.Text.UnderlineType UnderlineValue = global::Microsoft.UI.Text.UnderlineType.Undefined;
		internal bool ForegroundDefined;
		internal global::Windows.UI.Color ForegroundValue;
		internal bool SizeDefined;
		internal float SizeValue;
		internal bool NameDefined;
		internal string? NameValue;

		public global::Microsoft.UI.Text.FormatEffect Bold
		{
			get => BoldEffect;
			set
			{
				BoldEffect = value;
				ApplyIfBound();
			}
		}

		public global::Microsoft.UI.Text.FormatEffect Italic
		{
			get => ItalicEffect;
			set
			{
				ItalicEffect = value;
				ApplyIfBound();
			}
		}

		public global::Microsoft.UI.Text.FormatEffect Strikethrough
		{
			get => StrikethroughEffect;
			set
			{
				StrikethroughEffect = value;
				ApplyIfBound();
			}
		}

		public global::Microsoft.UI.Text.UnderlineType Underline
		{
			get => UnderlineValue;
			set
			{
				UnderlineValue = value;
				ApplyIfBound();
			}
		}

		public global::Windows.UI.Color ForegroundColor
		{
			get => ForegroundDefined ? ForegroundValue : default;
			set
			{
				ForegroundValue = value;
				ForegroundDefined = true;
				ApplyIfBound();
			}
		}

		public float Size
		{
			get => SizeDefined ? SizeValue : 0f;
			set
			{
				SizeValue = value;
				SizeDefined = true;
				ApplyIfBound();
			}
		}

		public string Name
		{
			get => NameValue ?? string.Empty;
			set
			{
				NameValue = value;
				NameDefined = true;
				ApplyIfBound();
			}
		}

		// Weight is aliased onto Bold: >= 600 (SemiBold and up) is treated as bold.
		public int Weight
		{
			get => BoldEffect switch
			{
				global::Microsoft.UI.Text.FormatEffect.On => 700,
				global::Microsoft.UI.Text.FormatEffect.Off => 400,
				_ => 0,
			};
			set
			{
				BoldEffect = value >= 600 ? global::Microsoft.UI.Text.FormatEffect.On : global::Microsoft.UI.Text.FormatEffect.Off;
				ApplyIfBound();
			}
		}

		// FontStyle is aliased onto Italic.
		public global::Windows.UI.Text.FontStyle FontStyle
		{
			get => ItalicEffect == global::Microsoft.UI.Text.FormatEffect.On
				? global::Windows.UI.Text.FontStyle.Italic
				: global::Windows.UI.Text.FontStyle.Normal;
			set
			{
				ItalicEffect = value != global::Windows.UI.Text.FontStyle.Normal
					? global::Microsoft.UI.Text.FormatEffect.On
					: global::Microsoft.UI.Text.FormatEffect.Off;
				ApplyIfBound();
			}
		}

		// --- Deferred surface: round-trips on this object but is not yet persisted per run or rendered. ---
		// TODO Uno: Persist and render these once the rich-content model is expanded.
		public global::Microsoft.UI.Text.FormatEffect AllCaps { get; set; } = global::Microsoft.UI.Text.FormatEffect.Undefined;

		public global::Windows.UI.Color BackgroundColor { get; set; }

		public global::Windows.UI.Text.FontStretch FontStretch { get; set; }

		public global::Microsoft.UI.Text.FormatEffect Hidden { get; set; } = global::Microsoft.UI.Text.FormatEffect.Undefined;

		public float Kerning { get; set; }

		public string LanguageTag { get; set; } = string.Empty;

		public global::Microsoft.UI.Text.LinkType LinkType => global::Microsoft.UI.Text.LinkType.Undefined;

		public global::Microsoft.UI.Text.FormatEffect Outline { get; set; } = global::Microsoft.UI.Text.FormatEffect.Undefined;

		public float Position { get; set; }

		public global::Microsoft.UI.Text.FormatEffect ProtectedText { get; set; } = global::Microsoft.UI.Text.FormatEffect.Undefined;

		public global::Microsoft.UI.Text.FormatEffect SmallCaps { get; set; } = global::Microsoft.UI.Text.FormatEffect.Undefined;

		public float Spacing { get; set; }

		public global::Microsoft.UI.Text.FormatEffect Subscript { get; set; } = global::Microsoft.UI.Text.FormatEffect.Undefined;

		public global::Microsoft.UI.Text.FormatEffect Superscript { get; set; } = global::Microsoft.UI.Text.FormatEffect.Undefined;

		public global::Microsoft.UI.Text.TextScript TextScript { get; set; }

		public global::Microsoft.UI.Text.ITextCharacterFormat GetClone()
		{
			var clone = new UnoTextCharacterFormat();
			clone.CopyFrom(this);
			return clone;
		}

		public void SetClone(global::Microsoft.UI.Text.ITextCharacterFormat value)
		{
			if (value is UnoTextCharacterFormat other)
			{
				CopyFrom(other);
			}
		}

		public bool IsEqual(global::Microsoft.UI.Text.ITextCharacterFormat format)
			=> format is UnoTextCharacterFormat other
				&& BoldEffect == other.BoldEffect
				&& ItalicEffect == other.ItalicEffect
				&& StrikethroughEffect == other.StrikethroughEffect
				&& UnderlineValue == other.UnderlineValue
				&& ForegroundDefined == other.ForegroundDefined
				&& ForegroundValue.Equals(other.ForegroundValue)
				&& SizeDefined == other.SizeDefined
				&& SizeValue.Equals(other.SizeValue)
				&& NameDefined == other.NameDefined
				&& string.Equals(NameValue, other.NameValue, StringComparison.Ordinal);

		private void CopyFrom(UnoTextCharacterFormat other)
		{
			BoldEffect = other.BoldEffect;
			ItalicEffect = other.ItalicEffect;
			StrikethroughEffect = other.StrikethroughEffect;
			UnderlineValue = other.UnderlineValue;
			ForegroundDefined = other.ForegroundDefined;
			ForegroundValue = other.ForegroundValue;
			SizeDefined = other.SizeDefined;
			SizeValue = other.SizeValue;
			NameDefined = other.NameDefined;
			NameValue = other.NameValue;
			AllCaps = other.AllCaps;
			BackgroundColor = other.BackgroundColor;
			FontStretch = other.FontStretch;
			Hidden = other.Hidden;
			Kerning = other.Kerning;
			LanguageTag = other.LanguageTag;
			Outline = other.Outline;
			Position = other.Position;
			ProtectedText = other.ProtectedText;
			SmallCaps = other.SmallCaps;
			Spacing = other.Spacing;
			Subscript = other.Subscript;
			Superscript = other.Superscript;
			TextScript = other.TextScript;
		}

		/// <summary>Populates the tracked subset from a single resolved run state (a degenerate range).</summary>
		internal void LoadFrom(CharacterFormatState state)
		{
			BoldEffect = state.Bold ? global::Microsoft.UI.Text.FormatEffect.On : global::Microsoft.UI.Text.FormatEffect.Off;
			ItalicEffect = state.Italic ? global::Microsoft.UI.Text.FormatEffect.On : global::Microsoft.UI.Text.FormatEffect.Off;
			StrikethroughEffect = state.Strikethrough ? global::Microsoft.UI.Text.FormatEffect.On : global::Microsoft.UI.Text.FormatEffect.Off;
			UnderlineValue = state.Underline;
			if (state.Foreground is { } fg)
			{
				ForegroundValue = fg;
				ForegroundDefined = true;
			}

			if (state.Size > 0)
			{
				SizeValue = state.Size;
				SizeDefined = true;
			}

			if (!string.IsNullOrEmpty(state.Name))
			{
				NameValue = state.Name;
				NameDefined = true;
			}
		}
	}
}
