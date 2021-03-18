#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class FaceDetectionEffect : global::Windows.Media.IMediaExtension
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Enabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool FaceDetectionEffect.Enabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.FaceDetectionEffect", "bool FaceDetectionEffect.Enabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan DesiredDetectionInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan FaceDetectionEffect.DesiredDetectionInterval is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.FaceDetectionEffect", "TimeSpan FaceDetectionEffect.DesiredDetectionInterval");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Core.FaceDetectionEffect.Enabled.set
		// Forced skipping of method Windows.Media.Core.FaceDetectionEffect.Enabled.get
		// Forced skipping of method Windows.Media.Core.FaceDetectionEffect.DesiredDetectionInterval.set
		// Forced skipping of method Windows.Media.Core.FaceDetectionEffect.DesiredDetectionInterval.get
		// Forced skipping of method Windows.Media.Core.FaceDetectionEffect.FaceDetected.add
		// Forced skipping of method Windows.Media.Core.FaceDetectionEffect.FaceDetected.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetProperties( global::Windows.Foundation.Collections.IPropertySet configuration)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.FaceDetectionEffect", "void FaceDetectionEffect.SetProperties(IPropertySet configuration)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Core.FaceDetectionEffect, global::Windows.Media.Core.FaceDetectedEventArgs> FaceDetected
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.FaceDetectionEffect", "event TypedEventHandler<FaceDetectionEffect, FaceDetectedEventArgs> FaceDetectionEffect.FaceDetected");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.FaceDetectionEffect", "event TypedEventHandler<FaceDetectionEffect, FaceDetectedEventArgs> FaceDetectionEffect.FaceDetected");
			}
		}
		#endif
		// Processing: Windows.Media.IMediaExtension
	}
}
