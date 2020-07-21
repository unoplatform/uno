#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ICorePointerInputSource 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool HasCapture
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Core.CoreCursor PointerCursor
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.Point PointerPosition
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void ReleasePointerCapture();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void SetPointerCapture();
		#endif
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.HasCapture.get
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerPosition.get
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerCursor.get
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerCursor.set
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerCaptureLost.add
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerCaptureLost.remove
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerEntered.add
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerEntered.remove
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerExited.add
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerExited.remove
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerMoved.add
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerMoved.remove
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerPressed.add
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerPressed.remove
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerReleased.add
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerReleased.remove
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerWheelChanged.add
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerWheelChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.PointerEventArgs> PointerCaptureLost;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.PointerEventArgs> PointerEntered;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.PointerEventArgs> PointerExited;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.PointerEventArgs> PointerMoved;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.PointerEventArgs> PointerPressed;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.PointerEventArgs> PointerReleased;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.PointerEventArgs> PointerWheelChanged;
		#endif
	}
}
