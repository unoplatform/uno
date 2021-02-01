#if NET461
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls
{
	abstract partial class VirtualizingPanelLayout
	{
		public abstract Orientation ScrollOrientation { get; }

		public Orientation Orientation { get; set; }

		Uno.UI.IndexPath? GetReorderingIndex() => null;
	}
}
#endif
