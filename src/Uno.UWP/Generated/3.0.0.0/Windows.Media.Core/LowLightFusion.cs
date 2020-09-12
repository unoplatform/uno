#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LowLightFusion 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static int MaxSupportedFrameCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member int LowLightFusion.MaxSupportedFrameCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<global::Windows.Graphics.Imaging.BitmapPixelFormat> SupportedBitmapPixelFormats
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<BitmapPixelFormat> LowLightFusion.SupportedBitmapPixelFormats is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Core.LowLightFusion.SupportedBitmapPixelFormats.get
		// Forced skipping of method Windows.Media.Core.LowLightFusion.MaxSupportedFrameCount.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Media.Core.LowLightFusionResult, double> FuseAsync( global::System.Collections.Generic.IEnumerable<global::Windows.Graphics.Imaging.SoftwareBitmap> frameSet)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<LowLightFusionResult, double> LowLightFusion.FuseAsync(IEnumerable<SoftwareBitmap> frameSet) is not implemented in Uno.");
		}
		#endif
	}
}
