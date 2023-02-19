#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ViewManagement.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreInputViewAnimationStartingEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreInputViewAnimationStartingEventArgs.Handled is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20CoreInputViewAnimationStartingEventArgs.Handled");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.Core.CoreInputViewAnimationStartingEventArgs", "bool CoreInputViewAnimationStartingEventArgs.Handled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan AnimationDuration
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan CoreInputViewAnimationStartingEventArgs.AnimationDuration is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=TimeSpan%20CoreInputViewAnimationStartingEventArgs.AnimationDuration");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.ViewManagement.Core.CoreInputViewOcclusion> Occlusions
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<CoreInputViewOcclusion> CoreInputViewAnimationStartingEventArgs.Occlusions is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CCoreInputViewOcclusion%3E%20CoreInputViewAnimationStartingEventArgs.Occlusions");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreInputViewAnimationStartingEventArgs.Occlusions.get
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreInputViewAnimationStartingEventArgs.Handled.get
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreInputViewAnimationStartingEventArgs.Handled.set
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreInputViewAnimationStartingEventArgs.AnimationDuration.get
	}
}
