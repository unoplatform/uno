#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ViewManagement.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreFrameworkInputViewAnimationStartingEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan AnimationDuration
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan CoreFrameworkInputViewAnimationStartingEventArgs.AnimationDuration is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=TimeSpan%20CoreFrameworkInputViewAnimationStartingEventArgs.AnimationDuration");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool FrameworkAnimationRecommended
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreFrameworkInputViewAnimationStartingEventArgs.FrameworkAnimationRecommended is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20CoreFrameworkInputViewAnimationStartingEventArgs.FrameworkAnimationRecommended");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.ViewManagement.Core.CoreInputViewOcclusion> Occlusions
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<CoreInputViewOcclusion> CoreFrameworkInputViewAnimationStartingEventArgs.Occlusions is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CCoreInputViewOcclusion%3E%20CoreFrameworkInputViewAnimationStartingEventArgs.Occlusions");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreFrameworkInputViewAnimationStartingEventArgs.Occlusions.get
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreFrameworkInputViewAnimationStartingEventArgs.FrameworkAnimationRecommended.get
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreFrameworkInputViewAnimationStartingEventArgs.AnimationDuration.get
	}
}
