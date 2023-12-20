#if !__WASM__
using System;
using System.Collections;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents.TextFormatting;

namespace Windows.UI.Xaml.Documents
{
	public partial class InlineCollection : SegmentsCollection<Inline>, IList<Inline>, IEnumerable<Inline>
	{
		internal InlineCollection(DependencyObject parent)
			: base(parent)
		{

		}

		private protected override void OnCollectionChanged()
		{
#if !IS_UNIT_TESTS
			base.OnCollectionChanged();
			switch (this.GetParent())
			{
				case ISegmentsElement segmentsElement:
					segmentsElement.InvalidateSegments();
					break;
				case Inline inline:
					inline.InvalidateSegments();
					break;
				case Block block:
					block.InvalidateSegments();
					break;
				default:
					break;
			}
#endif
		}
	}
}
#endif
