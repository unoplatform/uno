#nullable enable

using System;

namespace Microsoft.UI.Text
{
	// Uno-specific functional implementation of ITextCharacterFormat for the RichEditBox Text Object
	// Model. This is a tri-state value object: each property can be a concrete value or "undefined"
	// (FormatEffect.Undefined / UnderlineType.Undefined / an unset flag) when it is read over a range
	// whose characters disagree. Applying a format only writes back the properties that are defined.
	//
	// Every public property is persisted in the run model. Rendering projects the subset with an exact
	// shared-layout equivalent; other properties remain queryable and transferable through the TOM.
	internal sealed class UnoTextCharacterFormat : global::Microsoft.UI.Text.ITextCharacterFormat
	{
		// When bound, each tracked-property setter applies immediately through the bound callback: for a
		// range-bound format (via the range's CharacterFormat getter) this pushes into that range's
		// characters — making the canonical `range.CharacterFormat.Bold = On` idiom work, matching
		// WinUI's live character-format object; for the document default (GetDefaultCharacterFormat) it
		// writes the document's default state. A cloned or default-constructed format is unbound and
		// behaves as a plain value object.
		private Action<UnoTextCharacterFormat>? _apply;

		internal void Bind(UnoTextRange range) => _apply = range.ApplyCharacterFormat;

		internal void BindApply(Action<UnoTextCharacterFormat> apply) => _apply = apply;

		private void ApplyIfBound(Action<UnoTextCharacterFormat> define)
		{
			if (_apply is { } apply)
			{
				var delta = new UnoTextCharacterFormat();
				define(delta);
				apply(delta);
			}
		}

		private void ApplyAllIfBound() => _apply?.Invoke(this);

		internal global::Microsoft.UI.Text.FormatEffect AllCapsEffect = global::Microsoft.UI.Text.FormatEffect.Undefined;
		internal bool BackgroundDefined;
		internal bool BackgroundAutomatic;
		internal global::Windows.UI.Color BackgroundValue;
		internal global::Microsoft.UI.Text.FormatEffect BoldEffect = global::Microsoft.UI.Text.FormatEffect.Undefined;
		internal bool FontStretchDefined;
		internal global::Windows.UI.Text.FontStretch FontStretchValue;
		internal global::Microsoft.UI.Text.FormatEffect HiddenEffect = global::Microsoft.UI.Text.FormatEffect.Undefined;
		internal global::Microsoft.UI.Text.FormatEffect ItalicEffect = global::Microsoft.UI.Text.FormatEffect.Undefined;
		internal bool KerningDefined;
		internal float KerningValue;
		internal bool LanguageTagDefined;
		internal string LanguageTagValue = string.Empty;
		internal global::Microsoft.UI.Text.FormatEffect OutlineEffect = global::Microsoft.UI.Text.FormatEffect.Undefined;
		internal bool PositionDefined;
		internal float PositionValue;
		internal global::Microsoft.UI.Text.FormatEffect ProtectedTextEffect = global::Microsoft.UI.Text.FormatEffect.Undefined;
		internal global::Microsoft.UI.Text.FormatEffect SmallCapsEffect = global::Microsoft.UI.Text.FormatEffect.Undefined;
		internal bool SpacingDefined;
		internal float SpacingValue;
		internal global::Microsoft.UI.Text.FormatEffect StrikethroughEffect = global::Microsoft.UI.Text.FormatEffect.Undefined;
		internal global::Microsoft.UI.Text.FormatEffect SubscriptEffect = global::Microsoft.UI.Text.FormatEffect.Undefined;
		internal global::Microsoft.UI.Text.FormatEffect SuperscriptEffect = global::Microsoft.UI.Text.FormatEffect.Undefined;
		internal global::Microsoft.UI.Text.TextScript TextScriptValue = global::Microsoft.UI.Text.TextScript.Undefined;
		internal global::Microsoft.UI.Text.UnderlineType UnderlineValue = global::Microsoft.UI.Text.UnderlineType.Undefined;
		internal bool ForegroundDefined;
		internal bool ForegroundAutomatic;
		internal global::Windows.UI.Color ForegroundValue;
		internal bool SizeDefined;
		internal float SizeValue;
		internal bool NameDefined;
		internal string? NameValue;
		internal bool WeightDefined;
		internal int WeightValue;
		internal global::Microsoft.UI.Text.LinkType LinkTypeValue = global::Microsoft.UI.Text.LinkType.Undefined;

		public global::Microsoft.UI.Text.FormatEffect Bold
		{
			get => BoldEffect;
			set
			{
				BoldEffect = value;
				if (value is global::Microsoft.UI.Text.FormatEffect.On or global::Microsoft.UI.Text.FormatEffect.Off)
				{
					WeightValue = value == global::Microsoft.UI.Text.FormatEffect.On ? 700 : 400;
					WeightDefined = true;
				}
				else if (value == global::Microsoft.UI.Text.FormatEffect.Toggle)
				{
					WeightDefined = false;
				}
				ApplyIfBound(delta =>
				{
					delta.BoldEffect = value;
					delta.WeightDefined = WeightDefined;
					delta.WeightValue = WeightValue;
				});
			}
		}

		public global::Microsoft.UI.Text.FormatEffect Italic
		{
			get => ItalicEffect;
			set
			{
				ItalicEffect = value;
				ApplyIfBound(delta => delta.ItalicEffect = value);
			}
		}

		public global::Microsoft.UI.Text.FormatEffect Strikethrough
		{
			get => StrikethroughEffect;
			set
			{
				StrikethroughEffect = value;
				ApplyIfBound(delta => delta.StrikethroughEffect = value);
			}
		}

		public global::Microsoft.UI.Text.UnderlineType Underline
		{
			get => UnderlineValue;
			set
			{
				UnderlineValue = value;
				ApplyIfBound(delta => delta.UnderlineValue = value);
			}
		}

		public global::Windows.UI.Color ForegroundColor
		{
			get => ForegroundDefined
				? ForegroundAutomatic ? global::Microsoft.UI.Text.TextConstants.AutoColor : ForegroundValue
				: global::Microsoft.UI.Text.TextConstants.UndefinedColor;
			set
			{
				ForegroundValue = value;
				ForegroundDefined = value != global::Microsoft.UI.Text.TextConstants.UndefinedColor;
				ForegroundAutomatic = value == global::Microsoft.UI.Text.TextConstants.AutoColor;
				ApplyIfBound(delta =>
				{
					delta.ForegroundValue = value;
					delta.ForegroundDefined = ForegroundDefined;
					delta.ForegroundAutomatic = ForegroundAutomatic;
				});
			}
		}

		public float Size
		{
			get => SizeDefined ? SizeValue : global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
			set
			{
				SizeValue = value;
				SizeDefined = value != global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
				ApplyIfBound(delta =>
				{
					delta.SizeValue = value;
					delta.SizeDefined = SizeDefined;
				});
			}
		}

		public string Name
		{
			get => NameValue ?? string.Empty;
			set
			{
				NameValue = value;
				NameDefined = true;
				ApplyIfBound(delta =>
				{
					delta.NameValue = value;
					delta.NameDefined = true;
				});
			}
		}

		public int Weight
		{
			get => WeightDefined ? WeightValue : global::Microsoft.UI.Text.TextConstants.UndefinedInt32Value;
			set
			{
				if (value == global::Microsoft.UI.Text.TextConstants.UndefinedInt32Value)
				{
					WeightDefined = false;
					return;
				}

				if (value is < 0 or > 999)
				{
					throw new ArgumentOutOfRangeException(nameof(value));
				}

				WeightValue = value;
				WeightDefined = true;
				BoldEffect = value >= 600 ? global::Microsoft.UI.Text.FormatEffect.On : global::Microsoft.UI.Text.FormatEffect.Off;
				ApplyIfBound(delta =>
				{
					delta.WeightValue = value;
					delta.WeightDefined = true;
				});
			}
		}

		// FontStyle is aliased onto Italic.
		public global::Windows.UI.Text.FontStyle FontStyle
		{
			get => ItalicEffect == global::Microsoft.UI.Text.FormatEffect.Undefined
				? global::Microsoft.UI.Text.TextConstants.UndefinedFontStyle
				: ItalicEffect == global::Microsoft.UI.Text.FormatEffect.On
					? global::Windows.UI.Text.FontStyle.Italic
					: global::Windows.UI.Text.FontStyle.Normal;
			set
			{
				if (value == global::Microsoft.UI.Text.TextConstants.UndefinedFontStyle)
				{
					ItalicEffect = global::Microsoft.UI.Text.FormatEffect.Undefined;
					return;
				}

				ItalicEffect = value != global::Windows.UI.Text.FontStyle.Normal
					? global::Microsoft.UI.Text.FormatEffect.On
					: global::Microsoft.UI.Text.FormatEffect.Off;
				ApplyIfBound(delta => delta.ItalicEffect = ItalicEffect);
			}
		}

		public global::Microsoft.UI.Text.FormatEffect AllCaps
		{
			get => AllCapsEffect;
			set
			{
				AllCapsEffect = value;
				ApplyIfBound(delta => delta.AllCapsEffect = value);
			}
		}

		public global::Windows.UI.Color BackgroundColor
		{
			get => BackgroundDefined
				? BackgroundAutomatic ? global::Microsoft.UI.Text.TextConstants.AutoColor : BackgroundValue
				: global::Microsoft.UI.Text.TextConstants.UndefinedColor;
			set
			{
				BackgroundValue = value;
				BackgroundDefined = value != global::Microsoft.UI.Text.TextConstants.UndefinedColor;
				BackgroundAutomatic = value == global::Microsoft.UI.Text.TextConstants.AutoColor;
				ApplyIfBound(delta =>
				{
					delta.BackgroundValue = value;
					delta.BackgroundDefined = BackgroundDefined;
					delta.BackgroundAutomatic = BackgroundAutomatic;
				});
			}
		}

		public global::Windows.UI.Text.FontStretch FontStretch
		{
			get => FontStretchDefined ? FontStretchValue : global::Microsoft.UI.Text.TextConstants.UndefinedFontStretch;
			set
			{
				FontStretchValue = value;
				FontStretchDefined = value != global::Microsoft.UI.Text.TextConstants.UndefinedFontStretch;
				ApplyIfBound(delta =>
				{
					delta.FontStretchValue = value;
					delta.FontStretchDefined = FontStretchDefined;
				});
			}
		}

		public global::Microsoft.UI.Text.FormatEffect Hidden
		{
			get => HiddenEffect;
			set
			{
				HiddenEffect = value;
				ApplyIfBound(delta => delta.HiddenEffect = value);
			}
		}

		public float Kerning
		{
			get => KerningDefined ? KerningValue : global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
			set
			{
				KerningValue = value;
				KerningDefined = value != global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
				ApplyIfBound(delta =>
				{
					delta.KerningValue = value;
					delta.KerningDefined = KerningDefined;
				});
			}
		}

		public string LanguageTag
		{
			get => LanguageTagDefined ? LanguageTagValue : string.Empty;
			set
			{
				var normalized = value ?? string.Empty;
				if (normalized.Length > CharacterFormatState.MaxLanguageTagLength)
				{
					throw new ArgumentException("The language tag is too long.", nameof(value));
				}

				LanguageTagValue = normalized;
				LanguageTagDefined = true;
				ApplyIfBound(delta =>
				{
					delta.LanguageTagValue = LanguageTagValue;
					delta.LanguageTagDefined = true;
				});
			}
		}

		public global::Microsoft.UI.Text.LinkType LinkType => LinkTypeValue;

		public global::Microsoft.UI.Text.FormatEffect Outline
		{
			get => OutlineEffect;
			set
			{
				OutlineEffect = value;
				ApplyIfBound(delta => delta.OutlineEffect = value);
			}
		}

		public float Position
		{
			get => PositionDefined ? PositionValue : global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
			set
			{
				PositionValue = value;
				PositionDefined = value != global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
				ApplyIfBound(delta =>
				{
					delta.PositionValue = value;
					delta.PositionDefined = PositionDefined;
				});
			}
		}

		public global::Microsoft.UI.Text.FormatEffect ProtectedText
		{
			get => ProtectedTextEffect;
			set
			{
				ProtectedTextEffect = value;
				ApplyIfBound(delta => delta.ProtectedTextEffect = value);
			}
		}

		public global::Microsoft.UI.Text.FormatEffect SmallCaps
		{
			get => SmallCapsEffect;
			set
			{
				SmallCapsEffect = value;
				ApplyIfBound(delta => delta.SmallCapsEffect = value);
			}
		}

		public float Spacing
		{
			get => SpacingDefined ? SpacingValue : global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
			set
			{
				SpacingValue = value;
				SpacingDefined = value != global::Microsoft.UI.Text.TextConstants.UndefinedFloatValue;
				ApplyIfBound(delta =>
				{
					delta.SpacingValue = value;
					delta.SpacingDefined = SpacingDefined;
				});
			}
		}

		public global::Microsoft.UI.Text.FormatEffect Subscript
		{
			get => SubscriptEffect;
			set
			{
				SubscriptEffect = value;
				ApplyIfBound(delta => delta.SubscriptEffect = value);
			}
		}

		public global::Microsoft.UI.Text.FormatEffect Superscript
		{
			get => SuperscriptEffect;
			set
			{
				SuperscriptEffect = value;
				ApplyIfBound(delta => delta.SuperscriptEffect = value);
			}
		}

		public global::Microsoft.UI.Text.TextScript TextScript
		{
			get => TextScriptValue;
			set
			{
				TextScriptValue = value;
				ApplyIfBound(delta => delta.TextScriptValue = value);
			}
		}

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
				ApplyAllIfBound();
			}
		}

		public bool IsEqual(global::Microsoft.UI.Text.ITextCharacterFormat format)
			=> format is UnoTextCharacterFormat other
				&& AllCapsEffect == other.AllCapsEffect
				&& BackgroundDefined == other.BackgroundDefined
				&& BackgroundAutomatic == other.BackgroundAutomatic
				&& BackgroundValue.Equals(other.BackgroundValue)
				&& BoldEffect == other.BoldEffect
				&& FontStretchDefined == other.FontStretchDefined
				&& FontStretchValue == other.FontStretchValue
				&& HiddenEffect == other.HiddenEffect
				&& ItalicEffect == other.ItalicEffect
				&& KerningDefined == other.KerningDefined
				&& KerningValue.Equals(other.KerningValue)
				&& LanguageTagDefined == other.LanguageTagDefined
				&& string.Equals(LanguageTagValue, other.LanguageTagValue, StringComparison.Ordinal)
				&& OutlineEffect == other.OutlineEffect
				&& PositionDefined == other.PositionDefined
				&& PositionValue.Equals(other.PositionValue)
				&& ProtectedTextEffect == other.ProtectedTextEffect
				&& SmallCapsEffect == other.SmallCapsEffect
				&& SpacingDefined == other.SpacingDefined
				&& SpacingValue.Equals(other.SpacingValue)
				&& StrikethroughEffect == other.StrikethroughEffect
				&& SubscriptEffect == other.SubscriptEffect
				&& SuperscriptEffect == other.SuperscriptEffect
				&& TextScriptValue == other.TextScriptValue
				&& UnderlineValue == other.UnderlineValue
				&& ForegroundDefined == other.ForegroundDefined
				&& ForegroundAutomatic == other.ForegroundAutomatic
				&& ForegroundValue.Equals(other.ForegroundValue)
				&& SizeDefined == other.SizeDefined
				&& SizeValue.Equals(other.SizeValue)
				&& NameDefined == other.NameDefined
				&& string.Equals(NameValue, other.NameValue, StringComparison.Ordinal)
				&& WeightDefined == other.WeightDefined
				&& WeightValue == other.WeightValue
				&& LinkTypeValue == other.LinkTypeValue;

		private void CopyFrom(UnoTextCharacterFormat other)
		{
			AllCapsEffect = other.AllCapsEffect;
			BackgroundDefined = other.BackgroundDefined;
			BackgroundAutomatic = other.BackgroundAutomatic;
			BackgroundValue = other.BackgroundValue;
			BoldEffect = other.BoldEffect;
			FontStretchDefined = other.FontStretchDefined;
			FontStretchValue = other.FontStretchValue;
			HiddenEffect = other.HiddenEffect;
			ItalicEffect = other.ItalicEffect;
			KerningDefined = other.KerningDefined;
			KerningValue = other.KerningValue;
			LanguageTagDefined = other.LanguageTagDefined;
			LanguageTagValue = other.LanguageTagValue;
			OutlineEffect = other.OutlineEffect;
			PositionDefined = other.PositionDefined;
			PositionValue = other.PositionValue;
			ProtectedTextEffect = other.ProtectedTextEffect;
			SmallCapsEffect = other.SmallCapsEffect;
			SpacingDefined = other.SpacingDefined;
			SpacingValue = other.SpacingValue;
			StrikethroughEffect = other.StrikethroughEffect;
			SubscriptEffect = other.SubscriptEffect;
			SuperscriptEffect = other.SuperscriptEffect;
			TextScriptValue = other.TextScriptValue;
			UnderlineValue = other.UnderlineValue;
			ForegroundDefined = other.ForegroundDefined;
			ForegroundAutomatic = other.ForegroundAutomatic;
			ForegroundValue = other.ForegroundValue;
			SizeDefined = other.SizeDefined;
			SizeValue = other.SizeValue;
			NameDefined = other.NameDefined;
			NameValue = other.NameValue;
			WeightDefined = other.WeightDefined;
			WeightValue = other.WeightValue;
			LinkTypeValue = other.LinkTypeValue;
		}

		/// <summary>Populates the tracked subset from a single resolved run state (a degenerate range).</summary>
		internal void LoadFrom(CharacterFormatState state)
		{
			AllCapsEffect = Effect(state.AllCaps);
			BackgroundDefined = true;
			BackgroundAutomatic = state.Background is null;
			if (state.Background is { } background)
			{
				BackgroundValue = background;
				BackgroundDefined = true;
			}

			BoldEffect = Effect(state.Bold);
			WeightValue = state.Weight;
			WeightDefined = true;
			FontStretchValue = state.FontStretch;
			FontStretchDefined = true;
			HiddenEffect = Effect(state.Hidden);
			ItalicEffect = Effect(state.Italic);
			KerningValue = state.Kerning;
			KerningDefined = true;
			LanguageTagValue = state.LanguageTag;
			LanguageTagDefined = true;
			OutlineEffect = Effect(state.Outline);
			PositionValue = state.Position;
			PositionDefined = true;
			ProtectedTextEffect = Effect(state.ProtectedText);
			SmallCapsEffect = Effect(state.SmallCaps);
			SpacingValue = state.Spacing;
			SpacingDefined = true;
			StrikethroughEffect = Effect(state.Strikethrough);
			SubscriptEffect = Effect(state.Subscript);
			SuperscriptEffect = Effect(state.Superscript);
			TextScriptValue = state.TextScript;
			UnderlineValue = state.Underline;
			ForegroundDefined = true;
			ForegroundAutomatic = state.Foreground is null;
			if (state.Foreground is { } fg)
			{
				ForegroundValue = fg;
				ForegroundDefined = true;
			}

			SizeValue = state.Size;
			SizeDefined = true;

			NameValue = state.Name;
			NameDefined = true;

			LinkTypeValue = state.Link is null
				? global::Microsoft.UI.Text.LinkType.NotALink
				: global::Microsoft.UI.Text.LinkType.FriendlyLinkName;
		}

		private static global::Microsoft.UI.Text.FormatEffect Effect(bool value)
			=> value ? global::Microsoft.UI.Text.FormatEffect.On : global::Microsoft.UI.Text.FormatEffect.Off;
	}
}
