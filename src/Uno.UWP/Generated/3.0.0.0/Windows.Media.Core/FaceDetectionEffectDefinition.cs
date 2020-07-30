#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class FaceDetectionEffectDefinition : global::Windows.Media.Effects.IVideoEffectDefinition
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool SynchronousDetectionEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool FaceDetectionEffectDefinition.SynchronousDetectionEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.FaceDetectionEffectDefinition", "bool FaceDetectionEffectDefinition.SynchronousDetectionEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.FaceDetectionMode DetectionMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member FaceDetectionMode FaceDetectionEffectDefinition.DetectionMode is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.FaceDetectionEffectDefinition", "FaceDetectionMode FaceDetectionEffectDefinition.DetectionMode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ActivatableClassId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string FaceDetectionEffectDefinition.ActivatableClassId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Collections.IPropertySet Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IPropertySet FaceDetectionEffectDefinition.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public FaceDetectionEffectDefinition() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.FaceDetectionEffectDefinition", "FaceDetectionEffectDefinition.FaceDetectionEffectDefinition()");
		}
		#endif
		// Forced skipping of method Windows.Media.Core.FaceDetectionEffectDefinition.FaceDetectionEffectDefinition()
		// Forced skipping of method Windows.Media.Core.FaceDetectionEffectDefinition.ActivatableClassId.get
		// Forced skipping of method Windows.Media.Core.FaceDetectionEffectDefinition.Properties.get
		// Forced skipping of method Windows.Media.Core.FaceDetectionEffectDefinition.DetectionMode.set
		// Forced skipping of method Windows.Media.Core.FaceDetectionEffectDefinition.DetectionMode.get
		// Forced skipping of method Windows.Media.Core.FaceDetectionEffectDefinition.SynchronousDetectionEnabled.set
		// Forced skipping of method Windows.Media.Core.FaceDetectionEffectDefinition.SynchronousDetectionEnabled.get
		// Processing: Windows.Media.Effects.IVideoEffectDefinition
	}
}
