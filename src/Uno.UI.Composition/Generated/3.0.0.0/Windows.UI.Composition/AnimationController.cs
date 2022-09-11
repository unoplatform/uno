#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AnimationController : global::Windows.UI.Composition.CompositionObject
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Composition.AnimationControllerProgressBehavior ProgressBehavior
		{
			get
			{
				throw new global::System.NotImplementedException("The member AnimationControllerProgressBehavior AnimationController.ProgressBehavior is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.AnimationController", "AnimationControllerProgressBehavior AnimationController.ProgressBehavior");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float Progress
		{
			get
			{
				throw new global::System.NotImplementedException("The member float AnimationController.Progress is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.AnimationController", "float AnimationController.Progress");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float PlaybackRate
		{
			get
			{
				throw new global::System.NotImplementedException("The member float AnimationController.PlaybackRate is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.AnimationController", "float AnimationController.PlaybackRate");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static float MaxPlaybackRate
		{
			get
			{
				throw new global::System.NotImplementedException("The member float AnimationController.MaxPlaybackRate is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static float MinPlaybackRate
		{
			get
			{
				throw new global::System.NotImplementedException("The member float AnimationController.MinPlaybackRate is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.AnimationController.PlaybackRate.get
		// Forced skipping of method Windows.UI.Composition.AnimationController.PlaybackRate.set
		// Forced skipping of method Windows.UI.Composition.AnimationController.Progress.get
		// Forced skipping of method Windows.UI.Composition.AnimationController.Progress.set
		// Forced skipping of method Windows.UI.Composition.AnimationController.ProgressBehavior.get
		// Forced skipping of method Windows.UI.Composition.AnimationController.ProgressBehavior.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Pause()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.AnimationController", "void AnimationController.Pause()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Resume()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.AnimationController", "void AnimationController.Resume()");
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.AnimationController.MaxPlaybackRate.get
		// Forced skipping of method Windows.UI.Composition.AnimationController.MinPlaybackRate.get
	}
}
