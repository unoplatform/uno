#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI.DataBinding;

namespace Windows.UI.Xaml.Data
{
	[Flags]
	internal enum ResourceUpdateReason
	{
		None = 0,
		/// <summary>
		/// A static resource that could not be resolved at compile time, and should retry at loading. (This is something of a patch for compatibility gaps between Uno and WinUI)
		/// </summary>
		StaticResourceLoading = 1,
		/// <summary>
		/// An update associated with theme changing, or a resource binding that should be updated when theme changes
		/// </summary>
		ThemeResource = 2,
		/// <summary>
		/// An update associated with hot reload, or a resource binding that should be updated for hot-reload changes
		/// </summary>
		HotReload = 4,

		/// <summary>
		/// Update marked as XamlLoader
		/// </summary>
		XamlParser = 8,

		/// <summary>
		/// Updates that should be propagated recursively through the visual tree
		/// </summary>
		PropagatesThroughTree = ThemeResource | HotReload,
		/// <summary>
		/// Updates that should be re-resolved when the bound object or its parent is loaded into the visual tree
		/// </summary>
		ResolvedOnLoading = StaticResourceLoading | ThemeResource,
	}
}
