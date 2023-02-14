using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class GroupStyle
	{
		public DataTemplate HeaderTemplate { get; set; }

#if false || false || NET461 || __WASM__
		[Uno.NotImplemented]
#endif
		public DataTemplateSelector HeaderTemplateSelector { get; set; }

		public Style HeaderContainerStyle { get; set; }

		public bool HidesIfEmpty { get; set; }
	}
}
