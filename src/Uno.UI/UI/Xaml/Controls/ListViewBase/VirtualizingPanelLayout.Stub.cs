#if NET461
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	abstract partial class VirtualizingPanelLayout
	{
		public abstract Orientation ScrollOrientation { get; }

		public Orientation Orientation { get; set; }

		Uno.UI.IndexPath? GetAndUpdateReorderingIndex() => null;

		internal void UpdateReorderingItem(Point location, FrameworkElement element, object item) { }

		internal Uno.UI.IndexPath? CompleteReorderingItem(FrameworkElement element, object item) => null;

		internal void CleanupReordering() { }
	}
}
#endif
