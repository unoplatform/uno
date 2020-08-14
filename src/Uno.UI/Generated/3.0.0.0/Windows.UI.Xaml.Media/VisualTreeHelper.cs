#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class VisualTreeHelper 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.Xaml.Controls.Primitives.Popup> GetOpenPopupsForXamlRoot( global::Windows.UI.Xaml.XamlRoot xamlRoot)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<Popup> VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot xamlRoot) is not implemented in Uno.");
		}
		#endif
		// Skipping already declared method Windows.UI.Xaml.Media.VisualTreeHelper.GetOpenPopups(Windows.UI.Xaml.Window)
		// Skipping already declared method Windows.UI.Xaml.Media.VisualTreeHelper.FindElementsInHostCoordinates(Windows.Foundation.Point, Windows.UI.Xaml.UIElement)
		// Skipping already declared method Windows.UI.Xaml.Media.VisualTreeHelper.FindElementsInHostCoordinates(Windows.Foundation.Rect, Windows.UI.Xaml.UIElement)
		// Skipping already declared method Windows.UI.Xaml.Media.VisualTreeHelper.FindElementsInHostCoordinates(Windows.Foundation.Point, Windows.UI.Xaml.UIElement, bool)
		// Skipping already declared method Windows.UI.Xaml.Media.VisualTreeHelper.FindElementsInHostCoordinates(Windows.Foundation.Rect, Windows.UI.Xaml.UIElement, bool)
		// Skipping already declared method Windows.UI.Xaml.Media.VisualTreeHelper.GetChild(Windows.UI.Xaml.DependencyObject, int)
		// Skipping already declared method Windows.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(Windows.UI.Xaml.DependencyObject)
		// Skipping already declared method Windows.UI.Xaml.Media.VisualTreeHelper.GetParent(Windows.UI.Xaml.DependencyObject)
		// Skipping already declared method Windows.UI.Xaml.Media.VisualTreeHelper.DisconnectChildrenRecursive(Windows.UI.Xaml.UIElement)
	}
}
