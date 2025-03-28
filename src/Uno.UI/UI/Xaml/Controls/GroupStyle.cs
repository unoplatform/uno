using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class GroupStyle
	{
		public DataTemplate HeaderTemplate { get; set; }

#if false || false || IS_UNIT_TESTS || __WASM__
		[Uno.NotImplemented]
#endif
		public DataTemplateSelector HeaderTemplateSelector { get; set; }

		public Style HeaderContainerStyle { get; set; }

		public bool HidesIfEmpty { get; set; }
	}
}
