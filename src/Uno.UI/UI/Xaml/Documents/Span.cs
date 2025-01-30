using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Uno.Extensions.Specialized;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Documents
{
	[ContentProperty(Name = nameof(Inlines))]
	public partial class Span : Inline
	{
#if !__WASM__
		public Span()
		{
			Inlines = new InlineCollection(this);
		}
#endif

		public InlineCollection Inlines { get; set; }
	}
}
