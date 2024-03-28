using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// A panel which supports virtualization when used in <see cref="ListViewBase"/>. On Uno this is supported by delegating the panel's responsibilities
	/// to <see cref="NativeListViewBase"/>, which inherits from a native collection view.
	/// </summary>
	internal interface IVirtualizingPanel
	{
		/// <summary>
		/// Get a native layout object with the same layout configuration as the panel and is databound to the panel's properties.
		/// </summary>
		VirtualizingPanelLayout GetLayouter();
	}
}
