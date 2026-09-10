namespace Microsoft.UI.Xaml.Controls;

// TextFormatting partial for ContentPresenter.
// Mirrors WinUI's CContentPresenter::PullInheritedTextFormatting
// (ContentPresenter.cpp:1055-1130).
public partial class ContentPresenter
{
	private void OnTextFormattingPropertyChanged(string propertyName, object newValue)
	{
		if (TextFormattingHelper.IsProcessingInheritedNotification)
		{
			return;
		}

		var tf = _textFormatting ??= TextFormatting.CreateDefault();
		tf.SetFieldValue(propertyName, newValue);

		GlobalTextFormattingCounter.Invalidate();
		MarkInheritedPropertyDirty(propertyName, newValue);
	}

	/// <summary>
	/// Pulls inherited text formatting properties from parent.
	/// Each ContentPresenter-defined text property is only pulled if not set locally.
	/// MUX ref: CContentPresenter::PullInheritedTextFormatting (ContentPresenter.cpp:1055-1130).
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

		// MUX ref: ContentPresenter.cpp:1076 checks FrameworkElement_Language
		if (IsPropertyDefault(FrameworkElement.LanguageProperty))
		{
			_textFormatting.Language = parent.Language;
		}

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

		// CharacterSpacing — always copy (no DP on ContentPresenter)
		_textFormatting.CharacterSpacing = parent.CharacterSpacing;

		// TextDecorations — always copy (no DP on ContentPresenter)
		// MUX ref: ContentPresenter.cpp:1111 — unconditional copy
		_textFormatting.TextDecorations = parent.TextDecorations;

		if (IsPropertyDefault(FrameworkElement.FlowDirectionProperty))
		{
			_textFormatting.FlowDirection = parent.FlowDirection;
		}

		// MUX ref: ContentPresenter.cpp:1121 checks ContentPresenter_IsTextScaleFactorEnabled AND
		// FrameworkElement_IsTextScaleFactorEnabledInternal (Uno doesn't have the latter).
		if (IsPropertyDefault(ContentPresenter.IsTextScaleFactorEnabledProperty))
		{
			_textFormatting.IsTextScaleFactorEnabled = parent.IsTextScaleFactorEnabled;
		}
	}
}
