namespace Microsoft.UI.Xaml.Controls;

// TextFormatting partial for Control.
// Mirrors WinUI's CControl::PullInheritedTextFormatting (Control.cpp:116-191).
public partial class Control
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

		GlobalTextFormattingCounter.Invalidate();
		MarkInheritedPropertyDirty(propertyName, newValue);
	}

	/// <summary>
	/// Pulls inherited text formatting properties from parent.
	/// Each Control-defined text property is only pulled if not set locally.
	/// MUX ref: CControl::PullInheritedTextFormatting (Control.cpp:116-191).
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

		// MUX ref: Control.cpp:145 checks FrameworkElement_Language
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

		// Control doesn't have CharacterSpacing DP — always copy
		// MUX ref: Control.cpp:167 checks Control_CharacterSpacing (exists in WinUI but not Uno)
		_textFormatting.CharacterSpacing = parent.CharacterSpacing;

		// TextDecorations — always copy (no DP on Control)
		// MUX ref: Control.cpp:172 — unconditional copy
		_textFormatting.TextDecorations = parent.TextDecorations;

		if (IsPropertyDefault(FrameworkElement.FlowDirectionProperty))
		{
			_textFormatting.FlowDirection = parent.FlowDirection;
		}

		// MUX ref: Control.cpp:185 checks Control_IsTextScaleFactorEnabled AND
		// FrameworkElement_IsTextScaleFactorEnabledInternal (Uno doesn't have the latter).
		if (IsPropertyDefault(Control.IsTextScaleFactorEnabledProperty))
		{
			_textFormatting.IsTextScaleFactorEnabled = parent.IsTextScaleFactorEnabled;
		}
	}
}
