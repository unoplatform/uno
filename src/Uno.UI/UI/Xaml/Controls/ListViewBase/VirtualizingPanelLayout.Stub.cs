#if IS_UNIT_TESTS
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls
{
	abstract partial class VirtualizingPanelLayout
	{
		public abstract Orientation ScrollOrientation { get; }

		public Orientation Orientation { get; set; }

		internal void UpdateReorderingItem(Point location, FrameworkElement element, object item) { }

		internal Uno.UI.IndexPath? CompleteReorderingItem(FrameworkElement element, object item) => null;

		internal void CleanupReordering() { }
	}
}
#endif
