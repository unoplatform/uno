#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Imaging
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BitmapCodecInformation 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid CodecId
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid BitmapCodecInformation.CodecId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<string> FileExtensions
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<string> BitmapCodecInformation.FileExtensions is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string FriendlyName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string BitmapCodecInformation.FriendlyName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<string> MimeTypes
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<string> BitmapCodecInformation.MimeTypes is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Imaging.BitmapCodecInformation.CodecId.get
		// Forced skipping of method Windows.Graphics.Imaging.BitmapCodecInformation.FileExtensions.get
		// Forced skipping of method Windows.Graphics.Imaging.BitmapCodecInformation.FriendlyName.get
		// Forced skipping of method Windows.Graphics.Imaging.BitmapCodecInformation.MimeTypes.get
	}
}
