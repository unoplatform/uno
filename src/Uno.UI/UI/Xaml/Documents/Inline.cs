using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents.TextFormatting;

namespace Windows.UI.Xaml.Documents
{
	public abstract partial class Inline : TextElement
	{
		internal void InvalidateSegments()
		{
#if !IS_UNIT_TESTS
			var containingFrameworkElement = GetContainingFrameworkElement();
			if (containingFrameworkElement is ISegmentsElement segmentsElement)
			{
				segmentsElement.InvalidateSegments();
			}
#endif
		}

		internal void InvalidateElement()
		{
#if !IS_UNIT_TESTS
			var containingFrameworkElement = GetContainingFrameworkElement();
			if (containingFrameworkElement is ISegmentsElement segmentsElement)
			{
				segmentsElement.InvalidateElement();
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
