using Uno.UI.UI.Xaml.Documents;

namespace Windows.UI.Xaml.Documents
{
	partial class TextElement
	{
		protected TextElement(string htmlTag = "span") : base(htmlTag)
		{
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
	}
}
