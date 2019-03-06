#if !__WASM__
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Documents
{
	public partial class InlineCollection : DependencyObjectCollection<Inline>, IList<Inline>, IEnumerable<Inline>
	{
		internal InlineCollection(DependencyObject parent)
		{
			this.SetParent(parent);
		}

		private protected override void OnCollectionChanged()
		{
#if !NET461
			switch (this.GetParent())
			{
				case TextBlock textBlock:
					textBlock.InvalidateInlines();
					break;
				case Inline inline:
					inline.InvalidateInlines();
					break;
				default:
					break;
			}
#endif
		}
	}
}
#endif