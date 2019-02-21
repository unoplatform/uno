using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Documents
{
	public partial class Inline : TextElement
	{
		protected override void OnStyleChanged()
		{
			if (Style == null)
			{
				base.Style = Style.DefaultStyleForType(typeof(Inline));
				base.Style.ApplyTo(this);
			}
		}

		internal void InvalidateInlines()
		{
#if !NET461
			switch (this.GetParent())
			{
				case Span span:
					span.InvalidateInlines();
					break;
				case TextBlock textBlock:
					textBlock.InvalidateInlines();
					break;
				default:
					break;
			}
#endif
		}
	}
}
