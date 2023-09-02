using Uno.UI;
using Uno.UI.Xaml;
using Windows.UI.Xaml.Automation;

namespace Windows.UI.Xaml.Documents
{
	partial class TextElement
	{
		protected TextElement(string htmlTag = "span") : base(htmlTag)
		{
			SetDefaultForeground(ForegroundProperty);
		}

		partial void OnFontFamilyChangedPartial()
		{
			this.SetFontFamily(ReadLocalValue(FontFamilyProperty));
		}

		partial void OnFontStyleChangedPartial()
		{
			this.SetFontStyle(ReadLocalValue(FontStyleProperty));
		}

		partial void OnFontSizeChangedPartial()
		{
			this.SetFontSize(ReadLocalValue(FontSizeProperty));
		}

		partial void OnForegroundChangedPartial()
		{
			this.SetForeground(ReadLocalValue(ForegroundProperty));
		}

		partial void OnFontWeightChangedPartial()
		{
			this.SetFontWeight(ReadLocalValue(FontWeightProperty));
		}

		partial void OnCharacterSpacingChangedPartial()
		{
			this.SetCharacterSpacing(ReadLocalValue(CharacterSpacingProperty));
		}

		partial void OnBaseLineAlignmentChangedPartial()
		{

		}

		partial void OnTextDecorationsChangedPartial()
		{
			this.SetTextDecorations(ReadLocalValue(TextDecorationsProperty));
		}

		partial void OnNameChangedPartial(string newValue)
		{
			if (FrameworkElementHelper.IsUiAutomationMappingEnabled)
			{
				AutomationProperties.SetAutomationId(this, newValue);
			}

			if (FeatureConfiguration.UIElement.AssignDOMXamlName)
			{
				WindowManagerInterop.SetName(HtmlId, newValue);
			}
		}
	}
}
