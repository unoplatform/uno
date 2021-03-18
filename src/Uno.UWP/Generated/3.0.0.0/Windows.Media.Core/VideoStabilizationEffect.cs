#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VideoStabilizationEffect : global::Windows.Media.IMediaExtension
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Enabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool VideoStabilizationEffect.Enabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.VideoStabilizationEffect", "bool VideoStabilizationEffect.Enabled");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Core.VideoStabilizationEffect.Enabled.set
		// Forced skipping of method Windows.Media.Core.VideoStabilizationEffect.Enabled.get
		// Forced skipping of method Windows.Media.Core.VideoStabilizationEffect.EnabledChanged.add
		// Forced skipping of method Windows.Media.Core.VideoStabilizationEffect.EnabledChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Capture.VideoStreamConfiguration GetRecommendedStreamConfiguration( global::Windows.Media.Devices.VideoDeviceController controller,  global::Windows.Media.MediaProperties.VideoEncodingProperties desiredProperties)
		{
			throw new global::System.NotImplementedException("The member VideoStreamConfiguration VideoStabilizationEffect.GetRecommendedStreamConfiguration(VideoDeviceController controller, VideoEncodingProperties desiredProperties) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetProperties( global::Windows.Foundation.Collections.IPropertySet configuration)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.VideoStabilizationEffect", "void VideoStabilizationEffect.SetProperties(IPropertySet configuration)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Core.VideoStabilizationEffect, global::Windows.Media.Core.VideoStabilizationEffectEnabledChangedEventArgs> EnabledChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.VideoStabilizationEffect", "event TypedEventHandler<VideoStabilizationEffect, VideoStabilizationEffectEnabledChangedEventArgs> VideoStabilizationEffect.EnabledChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.VideoStabilizationEffect", "event TypedEventHandler<VideoStabilizationEffect, VideoStabilizationEffectEnabledChangedEventArgs> VideoStabilizationEffect.EnabledChanged");
			}
		}
		#endif
		// Processing: Windows.Media.IMediaExtension
	}
}
