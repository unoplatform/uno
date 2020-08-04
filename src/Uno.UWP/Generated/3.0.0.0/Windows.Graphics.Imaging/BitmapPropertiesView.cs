#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Imaging
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BitmapPropertiesView : global::Windows.Graphics.Imaging.IBitmapPropertiesView
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Graphics.Imaging.BitmapPropertySet> GetPropertiesAsync( global::System.Collections.Generic.IEnumerable<string> propertiesToRetrieve)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<BitmapPropertySet> BitmapPropertiesView.GetPropertiesAsync(IEnumerable<string> propertiesToRetrieve) is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Graphics.Imaging.IBitmapPropertiesView
	}
}
