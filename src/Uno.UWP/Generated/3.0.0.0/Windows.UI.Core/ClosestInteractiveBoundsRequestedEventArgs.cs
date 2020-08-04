#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ClosestInteractiveBoundsRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Rect ClosestInteractiveBounds
		{
			get
			{
				throw new global::System.NotImplementedException("The member Rect ClosestInteractiveBoundsRequestedEventArgs.ClosestInteractiveBounds is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.ClosestInteractiveBoundsRequestedEventArgs", "Rect ClosestInteractiveBoundsRequestedEventArgs.ClosestInteractiveBounds");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Point PointerPosition
		{
			get
			{
				throw new global::System.NotImplementedException("The member Point ClosestInteractiveBoundsRequestedEventArgs.PointerPosition is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Rect SearchBounds
		{
			get
			{
				throw new global::System.NotImplementedException("The member Rect ClosestInteractiveBoundsRequestedEventArgs.SearchBounds is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Core.ClosestInteractiveBoundsRequestedEventArgs.PointerPosition.get
		// Forced skipping of method Windows.UI.Core.ClosestInteractiveBoundsRequestedEventArgs.SearchBounds.get
		// Forced skipping of method Windows.UI.Core.ClosestInteractiveBoundsRequestedEventArgs.ClosestInteractiveBounds.get
		// Forced skipping of method Windows.UI.Core.ClosestInteractiveBoundsRequestedEventArgs.ClosestInteractiveBounds.set
	}
}
