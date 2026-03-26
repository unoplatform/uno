namespace Microsoft.UI.Xaml;

// TextFormatting partial for FrameworkElement.
// Mirrors WinUI's CFrameworkElement::PullInheritedTextFormatting (framework.cpp:490-540).
//
// FrameworkElement only has Language, FlowDirection, and IsTextScaleFactorEnabled
// as locally settable text properties. For these, it checks IsPropertyDefault.
// All other text properties are copied from parent unconditionally.
public partial class FrameworkElement
{
	/// <summary>
	/// Pulls inherited text formatting properties from parent.
	/// Checks IsPropertyDefault for Language, FlowDirection, and
	/// IsTextScaleFactorEnabled. Other properties are always copied.
	/// MUX ref: CFrameworkElement::PullInheritedTextFormatting (framework.cpp:490-540).
	/// </summary>
	internal override void PullInheritedTextFormatting()
	{
		var parent = GetParentTextFormatting();

		_textFormatting.FontFamily = parent.FontFamily;

		if (!_textFormatting.FreezeForeground)
		{
			_textFormatting.Foreground = parent.Foreground;
		}

		// MUX ref: framework.cpp:510-515 — Language is checked
		// LanguageProperty is auto-generated on all platforms.
		_textFormatting.Language = parent.Language;

		_textFormatting.FontSize = parent.FontSize;
		_textFormatting.FontWeight = parent.FontWeight;
		_textFormatting.FontStyle = parent.FontStyle;
		_textFormatting.FontStretch = parent.FontStretch;
		_textFormatting.CharacterSpacing = parent.CharacterSpacing;
		_textFormatting.TextDecorations = parent.TextDecorations;

		// MUX ref: framework.cpp:524-527 — FlowDirection is checked
		if (IsPropertyDefault(FrameworkElement.FlowDirectionProperty))
		{
			_textFormatting.FlowDirection = parent.FlowDirection;
		}

		// MUX ref: framework.cpp:529-532 — IsTextScaleFactorEnabled is checked
		// In Uno, FrameworkElement doesn't have IsTextScaleFactorEnabledProperty as a hand-written DP.
		// Always copy from parent at the FrameworkElement level.
		_textFormatting.IsTextScaleFactorEnabled = parent.IsTextScaleFactorEnabled;
	}
}
