#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Geometry : global::Windows.UI.Xaml.DependencyObject
	{
		// Skipping already declared property Transform
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Rect Bounds
		{
			get
			{
				throw new global::System.NotImplementedException("The member Rect Geometry.Bounds is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.Media.Geometry Empty
		{
			get
			{
				throw new global::System.NotImplementedException("The member Geometry Geometry.Empty is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static double StandardFlatteningTolerance
		{
			get
			{
				throw new global::System.NotImplementedException("The member double Geometry.StandardFlatteningTolerance is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property TransformProperty
		// Forced skipping of method Windows.UI.Xaml.Media.Geometry.Transform.get
		// Forced skipping of method Windows.UI.Xaml.Media.Geometry.Transform.set
		// Forced skipping of method Windows.UI.Xaml.Media.Geometry.Bounds.get
		// Forced skipping of method Windows.UI.Xaml.Media.Geometry.Empty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Geometry.StandardFlatteningTolerance.get
		// Forced skipping of method Windows.UI.Xaml.Media.Geometry.TransformProperty.get
	}
}
