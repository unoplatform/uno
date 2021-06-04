#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PathIconSource : global::Windows.UI.Xaml.Controls.IconSource
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Media.Geometry Data
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Geometry)this.GetValue(DataProperty);
			}
			set
			{
				this.SetValue(DataProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty DataProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Data), typeof(global::Windows.UI.Xaml.Media.Geometry), 
			typeof(global::Windows.UI.Xaml.Controls.PathIconSource), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Geometry)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PathIconSource() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PathIconSource", "PathIconSource.PathIconSource()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.PathIconSource.PathIconSource()
		// Forced skipping of method Windows.UI.Xaml.Controls.PathIconSource.Data.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PathIconSource.Data.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PathIconSource.DataProperty.get
	}
}
