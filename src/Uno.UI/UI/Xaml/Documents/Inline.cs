using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Documents
{
	public abstract partial class Inline : TextElement
	{
		internal void InvalidateInlines(bool updateText)
		{
#if !NET461
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
	}
}
