#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Controls
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class NonVirtualizingLayout : global::Microsoft.UI.Xaml.Controls.Layout
	{
		// Skipping already declared method Microsoft.UI.Xaml.Controls.NonVirtualizingLayout.NonVirtualizingLayout()
		// Forced skipping of method Microsoft.UI.Xaml.Controls.NonVirtualizingLayout.NonVirtualizingLayout()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		protected virtual void InitializeForContextCore( global::Microsoft.UI.Xaml.Controls.NonVirtualizingLayoutContext context)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.NonVirtualizingLayout", "void NonVirtualizingLayout.InitializeForContextCore(NonVirtualizingLayoutContext context)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		protected virtual void UninitializeForContextCore( global::Microsoft.UI.Xaml.Controls.NonVirtualizingLayoutContext context)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.NonVirtualizingLayout", "void NonVirtualizingLayout.UninitializeForContextCore(NonVirtualizingLayoutContext context)");
		}
		#endif
		// Skipping already declared method Microsoft.UI.Xaml.Controls.NonVirtualizingLayout.MeasureOverride(Microsoft.UI.Xaml.Controls.NonVirtualizingLayoutContext, Windows.Foundation.Size)
		// Skipping already declared method Microsoft.UI.Xaml.Controls.NonVirtualizingLayout.ArrangeOverride(Microsoft.UI.Xaml.Controls.NonVirtualizingLayoutContext, Windows.Foundation.Size)
	}
}
