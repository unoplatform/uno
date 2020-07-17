#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreComponentInputSource : global::Windows.UI.Core.ICoreInputSourceBase,global::Windows.UI.Core.ICorePointerInputSource,global::Windows.UI.Core.ICorePointerInputSource2
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool HasFocus
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreComponentInputSource.HasFocus is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsInputEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreComponentInputSource.IsInputEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "bool CoreComponentInputSource.IsInputEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Core.CoreDispatcher Dispatcher
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreDispatcher CoreComponentInputSource.Dispatcher is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Core.CoreCursor PointerCursor
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreCursor CoreComponentInputSource.PointerCursor is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "CoreCursor CoreComponentInputSource.PointerCursor");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool HasCapture
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreComponentInputSource.HasCapture is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Point PointerPosition
		{
			get
			{
				throw new global::System.NotImplementedException("The member Point CoreComponentInputSource.PointerPosition is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.DispatcherQueue DispatcherQueue
		{
			get
			{
				throw new global::System.NotImplementedException("The member DispatcherQueue CoreComponentInputSource.DispatcherQueue is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.Dispatcher.get
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.IsInputEnabled.get
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.IsInputEnabled.set
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.InputEnabled.add
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.InputEnabled.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ReleasePointerCapture()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "void CoreComponentInputSource.ReleasePointerCapture()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetPointerCapture()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "void CoreComponentInputSource.SetPointerCapture()");
		}
		#endif
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.HasCapture.get
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.PointerPosition.get
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.PointerCursor.get
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.PointerCursor.set
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.PointerCaptureLost.add
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.PointerCaptureLost.remove
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.PointerEntered.add
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.PointerEntered.remove
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.PointerExited.add
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.PointerExited.remove
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.PointerMoved.add
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.PointerMoved.remove
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.PointerPressed.add
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.PointerPressed.remove
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.PointerReleased.add
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.PointerReleased.remove
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.PointerWheelChanged.add
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.PointerWheelChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Core.CoreVirtualKeyStates GetCurrentKeyState( global::Windows.System.VirtualKey virtualKey)
		{
			throw new global::System.NotImplementedException("The member CoreVirtualKeyStates CoreComponentInputSource.GetCurrentKeyState(VirtualKey virtualKey) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.CharacterReceived.add
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.CharacterReceived.remove
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.KeyDown.add
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.KeyDown.remove
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.KeyUp.add
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.KeyUp.remove
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.HasFocus.get
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.GotFocus.add
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.GotFocus.remove
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.LostFocus.add
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.LostFocus.remove
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.TouchHitTesting.add
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.TouchHitTesting.remove
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.ClosestInteractiveBoundsRequested.add
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.ClosestInteractiveBoundsRequested.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string GetCurrentKeyEventDeviceId()
		{
			throw new global::System.NotImplementedException("The member string CoreComponentInputSource.GetCurrentKeyEventDeviceId() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Core.CoreComponentInputSource.DispatcherQueue.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.InputEnabledEventArgs> InputEnabled
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, InputEnabledEventArgs> CoreComponentInputSource.InputEnabled");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, InputEnabledEventArgs> CoreComponentInputSource.InputEnabled");
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
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreComponentInputSource.PointerCaptureLost");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreComponentInputSource.PointerCaptureLost");
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
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreComponentInputSource.PointerEntered");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreComponentInputSource.PointerEntered");
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
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreComponentInputSource.PointerExited");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreComponentInputSource.PointerExited");
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
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreComponentInputSource.PointerMoved");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreComponentInputSource.PointerMoved");
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
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreComponentInputSource.PointerPressed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreComponentInputSource.PointerPressed");
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
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreComponentInputSource.PointerReleased");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreComponentInputSource.PointerReleased");
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
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreComponentInputSource.PointerWheelChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, PointerEventArgs> CoreComponentInputSource.PointerWheelChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.CharacterReceivedEventArgs> CharacterReceived
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, CharacterReceivedEventArgs> CoreComponentInputSource.CharacterReceived");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, CharacterReceivedEventArgs> CoreComponentInputSource.CharacterReceived");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.KeyEventArgs> KeyDown
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, KeyEventArgs> CoreComponentInputSource.KeyDown");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, KeyEventArgs> CoreComponentInputSource.KeyDown");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.KeyEventArgs> KeyUp
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, KeyEventArgs> CoreComponentInputSource.KeyUp");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, KeyEventArgs> CoreComponentInputSource.KeyUp");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.CoreWindowEventArgs> GotFocus
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, CoreWindowEventArgs> CoreComponentInputSource.GotFocus");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, CoreWindowEventArgs> CoreComponentInputSource.GotFocus");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.CoreWindowEventArgs> LostFocus
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, CoreWindowEventArgs> CoreComponentInputSource.LostFocus");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, CoreWindowEventArgs> CoreComponentInputSource.LostFocus");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.TouchHitTestingEventArgs> TouchHitTesting
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, TouchHitTestingEventArgs> CoreComponentInputSource.TouchHitTesting");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<object, TouchHitTestingEventArgs> CoreComponentInputSource.TouchHitTesting");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreComponentInputSource, global::Windows.UI.Core.ClosestInteractiveBoundsRequestedEventArgs> ClosestInteractiveBoundsRequested
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<CoreComponentInputSource, ClosestInteractiveBoundsRequestedEventArgs> CoreComponentInputSource.ClosestInteractiveBoundsRequested");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreComponentInputSource", "event TypedEventHandler<CoreComponentInputSource, ClosestInteractiveBoundsRequestedEventArgs> CoreComponentInputSource.ClosestInteractiveBoundsRequested");
			}
		}
		#endif
		// Processing: Windows.UI.Core.ICoreInputSourceBase
		// Processing: Windows.UI.Core.ICorePointerInputSource
		// Processing: Windows.UI.Core.ICorePointerInputSource2
	}
}
