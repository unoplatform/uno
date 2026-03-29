using Microsoft.UI.Xaml.Controls;
using Uno.UI.DataBinding;
using Windows.UI.Text;
using TF = Microsoft.UI.Xaml.TextFormatting;

namespace Microsoft.UI.Xaml.Documents;

// TextFormatting partial for TextElement.
// Mirrors WinUI's CTextElement::PullInheritedTextFormatting (TextElement.cpp:184-263).
//
// On WASM, TextElement inherits from UIElement and gets _textFormatting from there.
// On all other platforms, TextElement inherits from DependencyObject and needs its
// own TextFormatting storage.
//
// Uses type alias TF to avoid collision with the Microsoft.UI.Xaml.Documents.TextFormatting namespace.
public abstract partial class TextElement : ITextFormattingOwner
{
#if !__WASM__
	// On non-WASM platforms, TextElement does NOT inherit from UIElement,
	// so it needs its own TextFormatting field.
	internal TF _textFormatting;
#endif

	TF ITextFormattingOwner.TextFormatting =>
#if __WASM__
		// On WASM, UIElement._textFormatting is used (inherited)
		((UIElement)(object)this)._textFormatting;
#else
		_textFormatting;
#endif

	void ITextFormattingOwner.EnsureTextFormatting(DependencyProperty property, bool forGetValue)
		=> EnsureTextFormatting(property, forGetValue);

	internal void EnsureTextFormatting(DependencyProperty property, bool forGetValue)
	{
#if __WASM__
		// On WASM, delegate to UIElement.EnsureTextFormatting
		((UIElement)(object)this).EnsureTextFormatting(property, forGetValue);
#else
		if (_textFormatting is null)
		{
			_textFormatting = TF.CreateDefault();
		}

		if (forGetValue
			&& _textFormatting.IsOld
			&& (property is null || IsPropertyDefault(property)))
		{
			PullInheritedTextFormatting();
			_textFormatting.SetIsUpToDate();
		}
#endif
	}

	/// <summary>
	/// Gets the parent TextFormatting by walking the inline parent chain.
	/// TextElement parents: Run -> Span -> TextBlock (via InlineCollection).
	/// MUX ref: CDependencyObject::GetParentTextFormatting (depends.cpp:3265-3309).
	/// </summary>
	internal TF GetParentTextFormatting()
	{
		var parent = this.GetParent();

		while (parent is not null)
		{
			TF tf = parent switch
			{
				UIElement uie => uie._textFormatting,
				TextElement te => te._textFormatting,
				_ => null
			};

			if (tf is not null)
			{
				if (parent is UIElement puie)
				{
					puie.EnsureTextFormatting(null, forGetValue: true);
				}
				else if (parent is TextElement pte)
				{
					pte.EnsureTextFormatting(null, forGetValue: true);
				}

				return tf;
			}

			parent = parent.GetParent();
		}

		return TF.CreateDefault();
	}

	/// <summary>
	/// Pulls all inherited text formatting properties from the parent.
	/// Checks IsPropertyDefault for each TextElement-specific property.
	/// MUX ref: CTextElement::PullInheritedTextFormatting (TextElement.cpp:184-263).
	/// </summary>
	internal virtual void PullInheritedTextFormatting()
	{
		var tf =
#if __WASM__
			((UIElement)(object)this)._textFormatting;
#else
			_textFormatting;
#endif

		if (tf is null)
		{
			return;
		}

		var parent = GetParentTextFormatting();

		if (IsPropertyDefault(FontFamilyProperty))
		{
			tf.FontFamily = parent.FontFamily;
		}

		if (IsPropertyDefault(ForegroundProperty) && !tf.FreezeForeground)
		{
			tf.Foreground = parent.Foreground;
		}

		if (IsPropertyDefault(FontSizeProperty))
		{
			tf.FontSize = parent.FontSize;
		}

		if (IsPropertyDefault(FontWeightProperty))
		{
			tf.FontWeight = parent.FontWeight;
		}

		if (IsPropertyDefault(FontStyleProperty))
		{
			tf.FontStyle = parent.FontStyle;
		}

		if (IsPropertyDefault(FontStretchProperty))
		{
			tf.FontStretch = parent.FontStretch;
		}

		if (IsPropertyDefault(CharacterSpacingProperty))
		{
			tf.CharacterSpacing = parent.CharacterSpacing;
		}

		if (IsPropertyDefault(TextDecorationsProperty))
		{
			tf.TextDecorations = parent.TextDecorations;
		}

		// MUX ref: TextElement.cpp:208 checks TextElement_Language
		if (IsPropertyDefault(LanguageProperty))
		{
			tf.Language = parent.Language;
		}

		// FlowDirection: In WinUI, only Run has FlowDirection on TextElement
		// (TextElement.cpp:242-246). In Uno, Run doesn't have FlowDirectionProperty,
		// so we always copy from parent.
		tf.FlowDirection = parent.FlowDirection;

		// MUX ref: TextElement.cpp:248 checks TextElement_IsTextScaleFactorEnabled
		if (IsPropertyDefault(IsTextScaleFactorEnabledProperty))
		{
			tf.IsTextScaleFactorEnabled = parent.IsTextScaleFactorEnabled;
		}
	}

	/// <summary>
	/// Called from PropertyChanged callbacks for text formatting DPs.
	/// Updates the TextFormatting storage and invalidates the global counter.
	/// MUX ref: CTextElement::MarkInheritedPropertyDirty (TextElement.cpp:271-280).
	/// </summary>
	private void OnTextFormattingPropertyChanged(string propertyName, object newValue)
	{
		if (TextFormattingHelper.IsProcessingInheritedNotification)
		{
			return;
		}

		var tf =
#if __WASM__
			((UIElement)(object)this)._textFormatting;
#else
			_textFormatting;
#endif

		if (tf is null)
		{
			tf = TF.CreateDefault();
#if __WASM__
			((UIElement)(object)this)._textFormatting = tf;
#else
			_textFormatting = tf;
#endif
		}

		tf.SetFieldValue(propertyName, newValue);

		// MUX ref: InvalidateInheritedProperty — depends.cpp:2706
		GlobalTextFormattingCounter.Invalidate();

		// TextElement children are inline children (Run inside Span/Hyperlink).
		// The generation counter invalidation is sufficient for them to re-pull
		// on next access via EnsureTextFormatting.
	}

	/// <summary>
	/// Returns true if the given DP has no value set at any precedence.
	/// </summary>
	internal bool IsPropertyDefault(DependencyProperty property)
	{
		if (property is null)
		{
			return true;
		}

		return DependencyObjectExtensions.GetCurrentHighestValuePrecedence(this, property)
			== DependencyPropertyValuePrecedences.DefaultValue;
	}
}
