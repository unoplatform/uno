#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Casting
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CastingSource 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri PreferredSourceUri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri CastingSource.PreferredSourceUri is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Casting.CastingSource", "Uri CastingSource.PreferredSourceUri");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Casting.CastingSource.PreferredSourceUri.get
		// Forced skipping of method Windows.Media.Casting.CastingSource.PreferredSourceUri.set
	}
}
