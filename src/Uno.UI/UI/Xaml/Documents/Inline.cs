using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Documents
{
	public abstract partial class Inline : TextElement
	{
		internal void InvalidateInlines(bool updateText) => InvalidateInlines(updateText, inherited: false);

		internal void InvalidateInlines(bool updateText, bool inherited)
		{
#if !IS_UNIT_TESTS
			switch (this.GetParent())
			{
				case Span span:
					span.InvalidateInlines(updateText, inherited);
					break;
				case TextBlock textBlock:
					textBlock.InvalidateInlines(updateText);
					break;
				case Block block:
					block.InvalidateInlines(contentChanged: !inherited);
					break;
				default:
					break;
			}
#endif
		}

		// CUIElement::MarkInheritedPropertyDirty walks only GetChildren(), so an inherited formatting change
		// on the owning RichTextBlock never reaches CTextElement::MarkDirty -> CRichTextBlock::OnContentChanged;
		// the owner just runs its own InvalidateContent. A locally set value does go through MarkDirty, and
		// must keep clearing the selection and the cached focusable children.
		private protected void InvalidateInlinesForFormatChange(DependencyProperty property)
			=> InvalidateInlines(
				updateText: false,
				inherited: this.GetCurrentHighestValuePrecedence(property) == DependencyPropertyValuePrecedences.Inheritance);

#if __NETSTD_REFERENCE__
		protected override void OnFontFamilyChanged() => base.OnFontFamilyChanged();

		protected override void OnFontStyleChanged() => base.OnFontStyleChanged();

		protected override void OnFontWeightChanged() => base.OnFontWeightChanged();

		protected override void OnFontSizeChanged() => base.OnFontSizeChanged();

		protected override void OnFontStretchChanged() => base.OnFontStretchChanged();
#endif
	}
}
