using System;
using System.Linq;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	/// <summary>
	/// This interface allows use to replicate the "DeriveFromPanelHelper_base" of WinUI (cf. Remarks)
	/// </summary>
	/// <remarks>
	/// Doc about the DeriveFromPanelHelper_base in WinUI: 
	/// This type exists for types that in metadata derive from FrameworkElement but internally want to derive from Panel
	/// to get "protected" Children.
	/// </remarks>
	internal partial interface IPanel
	{
		public UIElementCollection Children { get; }
	}
}
