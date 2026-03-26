namespace Microsoft.UI.Xaml.Controls;

// TextFormatting partial for TextBlock.
// Mirrors WinUI's CTextBlock::PullInheritedTextFormatting (TextBlock.cpp:628-706)
// and CTextBlock::MarkInheritedPropertyDirty (TextBlock.cpp:2038-2060).
public partial class TextBlock
{
	/// <summary>
	/// Called from PropertyChanged callbacks for text formatting DPs.
	/// Updates the TextFormatting storage, invalidates the global counter,
	/// and pushes notifications to children via MarkInheritedPropertyDirty.
	/// </summary>
	private void OnTextFormattingPropertyChanged(string propertyName, object newValue)
	{
		if (TextFormattingHelper.IsProcessingInheritedNotification)
		{
			return;
		}

		var tf = _textFormatting ??= TextFormatting.CreateDefault();
		tf.SetFieldValue(propertyName, newValue);

		// MUX ref: InvalidateInheritedProperty — depends.cpp:2706
		GlobalTextFormattingCounter.Invalidate();

		// MUX ref: MarkInheritedPropertyDirty — uielement.cpp:14408
		MarkInheritedPropertyDirty(propertyName, newValue);
	}

	/// <summary>
	/// Pulls inherited text formatting properties from parent.
	/// Each property is only pulled if not set locally (IsPropertyDefault).
	/// MUX ref: CTextBlock::PullInheritedTextFormatting (TextBlock.cpp:628-706).
	/// </summary>
	internal override void PullInheritedTextFormatting()
	{
		var parent = GetParentTextFormatting();

		if (IsPropertyDefault(FontFamilyProperty))
		{
			_textFormatting.FontFamily = parent.FontFamily;
		}

		if (IsPropertyDefault(ForegroundProperty) && !_textFormatting.FreezeForeground)
		{
			_textFormatting.Foreground = parent.Foreground;
		}

		// Language is on FrameworkElement — always copy at TextBlock level
		// MUX ref: TextBlock.cpp:652 checks FrameworkElement_Language
		_textFormatting.Language = parent.Language;

		if (IsPropertyDefault(FontSizeProperty))
		{
			_textFormatting.FontSize = parent.FontSize;
		}

		if (IsPropertyDefault(FontWeightProperty))
		{
			_textFormatting.FontWeight = parent.FontWeight;
		}

		if (IsPropertyDefault(FontStyleProperty))
		{
			_textFormatting.FontStyle = parent.FontStyle;
		}

		if (IsPropertyDefault(FontStretchProperty))
		{
			_textFormatting.FontStretch = parent.FontStretch;
		}

		if (IsPropertyDefault(CharacterSpacingProperty))
		{
			_textFormatting.CharacterSpacing = parent.CharacterSpacing;
		}

		if (IsPropertyDefault(TextDecorationsProperty))
		{
			_textFormatting.TextDecorations = parent.TextDecorations;
		}

		if (IsPropertyDefault(FrameworkElement.FlowDirectionProperty))
		{
			_textFormatting.FlowDirection = parent.FlowDirection;
		}

		// IsTextScaleFactorEnabled — always copy (no DP on TextBlock)
		_textFormatting.IsTextScaleFactorEnabled = parent.IsTextScaleFactorEnabled;
	}

	/// <summary>
	/// Overrides MarkInheritedPropertyDirty for TextBlock-specific invalidation.
	/// MUX ref: CTextBlock::MarkInheritedPropertyDirty (TextBlock.cpp:2038-2060).
	/// - Foreground → InvalidateRender (render-only, no relayout)
	/// - Other text properties → InvalidateTextLayout (full text layout rebuild)
	/// </summary>
	internal override void MarkInheritedPropertyDirty(string propertyName, object newValue)
	{
		base.MarkInheritedPropertyDirty(propertyName, newValue);

		if (propertyName == "Foreground")
		{
			// MUX ref: TextBlock.cpp:2057 — InvalidateRender()
			// In Uno, TextBlock uses InvalidateTextBlock() for all changes.
			// Foreground-only invalidation would be a render-only path,
			// but Uno doesn't currently distinguish render vs layout invalidation
			// at the TextBlock level.
			InvalidateTextBlock();
		}
		else
		{
			// MUX ref: TextBlock.cpp:2054 — InvalidateContent()
			InvalidateTextBlock();
		}
	}

	/// <summary>
	/// Propagates inherited text property changes to inline children (Run, Span, etc.).
	/// Called from UIElement.MarkInheritedPropertyDirty when the element is a TextBlock.
	/// </summary>
	internal void MarkInheritedPropertyDirtyForInlines(string propertyName, object newValue)
	{
		// TextBlock's Inlines collection contains TextElement children.
		// In WinUI, MarkInheritedPropertyDirty walks through CInlineCollection.
		// TextElement uses the DP system for inline inheritance, so the generation
		// counter invalidation is sufficient for them to re-pull on next access.
		// No additional work needed here — TextElement.EnsureTextFormatting will
		// detect IsOld and pull when a text property is read.
	}
}
