#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Controls
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class VirtualizingLayout : global::Microsoft.UI.Xaml.Controls.Layout
	{
		// Skipping already declared method Microsoft.UI.Xaml.Controls.VirtualizingLayout.VirtualizingLayout()
		// Forced skipping of method Microsoft.UI.Xaml.Controls.VirtualizingLayout.VirtualizingLayout()
		// Skipping already declared method Microsoft.UI.Xaml.Controls.VirtualizingLayout.InitializeForContextCore(Microsoft.UI.Xaml.Controls.VirtualizingLayoutContext)
		// Skipping already declared method Microsoft.UI.Xaml.Controls.VirtualizingLayout.UninitializeForContextCore(Microsoft.UI.Xaml.Controls.VirtualizingLayoutContext)
		// Skipping already declared method Microsoft.UI.Xaml.Controls.VirtualizingLayout.MeasureOverride(Microsoft.UI.Xaml.Controls.VirtualizingLayoutContext, Windows.Foundation.Size)
		// Skipping already declared method Microsoft.UI.Xaml.Controls.VirtualizingLayout.ArrangeOverride(Microsoft.UI.Xaml.Controls.VirtualizingLayoutContext, Windows.Foundation.Size)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		protected virtual void OnItemsChangedCore( global::Microsoft.UI.Xaml.Controls.VirtualizingLayoutContext context,  object source,  global::Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs args)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.VirtualizingLayout", "void VirtualizingLayout.OnItemsChangedCore(VirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)");
		}
		#endif
	}
}
