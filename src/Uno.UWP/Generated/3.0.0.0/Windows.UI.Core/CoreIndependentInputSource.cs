#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreIndependentInputSource : global::Windows.UI.Core.ICoreInputSourceBase,global::Windows.UI.Core.ICorePointerInputSource,global::Windows.UI.Core.ICorePointerInputSource2
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsInputEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreIndependentInputSource.IsInputEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSource", "bool CoreIndependentInputSource.IsInputEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Core.CoreDispatcher Dispatcher
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreDispatcher CoreIndependentInputSource.Dispatcher is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Core.CoreCursor PointerCursor
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreCursor CoreIndependentInputSource.PointerCursor is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSource", "CoreCursor CoreIndependentInputSource.PointerCursor");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool HasCapture
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreIndependentInputSource.HasCapture is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Point PointerPosition
		{
			get
			{
				throw new global::System.NotImplementedException("The member Point CoreIndependentInputSource.PointerPosition is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.DispatcherQueue DispatcherQueue
		{
			get
			{
				throw new global::System.NotImplementedException("The member DispatcherQueue CoreIndependentInputSource.DispatcherQueue is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.Dispatcher.get
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.IsInputEnabled.get
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.IsInputEnabled.set
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.InputEnabled.add
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.InputEnabled.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ReleasePointerCapture()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSource", "void CoreIndependentInputSource.ReleasePointerCapture()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetPointerCapture()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSource", "void CoreIndependentInputSource.SetPointerCapture()");
		}
		#endif
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.HasCapture.get
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.PointerPosition.get
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.PointerCursor.get
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.PointerCursor.set
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.PointerCaptureLost.add
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.PointerCaptureLost.remove
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.PointerEntered.add
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.PointerEntered.remove
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.PointerExited.add
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.PointerExited.remove
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.PointerMoved.add
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.PointerMoved.remove
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.PointerPressed.add
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.PointerPressed.remove
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.PointerReleased.add
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.PointerReleased.remove
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.PointerWheelChanged.add
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.PointerWheelChanged.remove
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSource.DispatcherQueue.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.InputEnabledEventArgs> InputEnabled
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSource", "event TypedEventHandler<object, InputEnabledEventArgs> CoreIndependentInputSource.InputEnabled");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSource", "event TypedEventHandler<object, InputEnabledEventArgs> CoreIndependentInputSource.InputEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.PointerEventArgs> PointerCaptureLost
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreIndependentInputSource.PointerCaptureLost");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreIndependentInputSource.PointerCaptureLost");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.PointerEventArgs> PointerEntered
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreIndependentInputSource.PointerEntered");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreIndependentInputSource.PointerEntered");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.PointerEventArgs> PointerExited
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreIndependentInputSource.PointerExited");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreIndependentInputSource.PointerExited");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.PointerEventArgs> PointerMoved
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreIndependentInputSource.PointerMoved");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreIndependentInputSource.PointerMoved");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.PointerEventArgs> PointerPressed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreIndependentInputSource.PointerPressed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreIndependentInputSource.PointerPressed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.PointerEventArgs> PointerReleased
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreIndependentInputSource.PointerReleased");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreIndependentInputSource.PointerReleased");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.PointerEventArgs> PointerWheelChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreIndependentInputSource.PointerWheelChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreIndependentInputSource.PointerWheelChanged");
			}
		}
		#endif
		// Processing: Windows.UI.Core.ICoreInputSourceBase
		// Processing: Windows.UI.Core.ICorePointerInputSource
		// Processing: Windows.UI.Core.ICorePointerInputSource2
	}
}
