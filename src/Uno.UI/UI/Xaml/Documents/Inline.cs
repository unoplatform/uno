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
		}

		// CUIElement::MarkInheritedPropertyDirty walks only GetChildren(), so an inherited formatting change
		// on the owning RichTextBlock never reaches CTextElement::MarkDirty -> CRichTextBlock::OnContentChanged;
		// the owner just runs its own InvalidateContent. Whether the change is also a content change is decided
		// in OnPropertyChanged2, which knows where the value came from.
		private protected void InvalidateInlinesForFormatChange() => InvalidateInlines(updateText: false, inherited: true);

		// CTextElement::SetValue -> MarkDirty: a value set on or cleared from this element, as opposed to one it only
		// inherits, is a content change. ClearValue takes that path too, even when the element inherits a value again.
		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);

			if (IsContentFormattingProperty(args.Property) &&
				(args.OldPrecedence < DependencyPropertyValuePrecedences.Inheritance || args.NewPrecedence < DependencyPropertyValuePrecedences.Inheritance))
			{
				InvalidateInlines(updateText: false, inherited: false);
			}
		}

		// CRichTextBlock::OnContentChanged only re-renders for a Foreground change.
		private static bool IsContentFormattingProperty(DependencyProperty property)
			=> property == FontFamilyProperty
			|| property == FontSizeProperty
			|| property == FontStyleProperty
			|| property == FontStretchProperty
			|| property == FontWeightProperty
			|| property == CharacterSpacingProperty
			|| property == TextDecorationsProperty
			|| property == IsTextScaleFactorEnabledProperty
			|| property == BaseLineAlignmentProperty
			|| property == Run.FlowDirectionProperty;
#if __NETSTD_REFERENCE__
		protected override void OnFontFamilyChanged() => base.OnFontFamilyChanged();

		protected override void OnFontStyleChanged() => base.OnFontStyleChanged();

		protected override void OnFontWeightChanged() => base.OnFontWeightChanged();

		protected override void OnFontSizeChanged() => base.OnFontSizeChanged();

		protected override void OnFontStretchChanged() => base.OnFontStretchChanged();
#endif
	}
}
