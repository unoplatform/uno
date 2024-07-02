using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Documents
{
	public abstract partial class Inline : TextElement
	{
		internal void InvalidateInlines(bool updateText)
		{
#if !IS_UNIT_TESTS
			switch (this.GetParent())
			{
				case Span span:
					span.InvalidateInlines(updateText);
					break;
				case TextBlock textBlock:
					textBlock.InvalidateInlines(updateText);
					break;
				default:
					break;
			}
#endif
		}

#if __WASM__ || __NETSTD_REFERENCE__
		protected override void OnFontFamilyChanged() => base.OnFontFamilyChanged();

		protected override void OnFontStyleChanged() => base.OnFontStyleChanged();

		protected override void OnFontWeightChanged() => base.OnFontWeightChanged();

		protected override void OnFontSizeChanged() => base.OnFontSizeChanged();
#endif
	}
}
