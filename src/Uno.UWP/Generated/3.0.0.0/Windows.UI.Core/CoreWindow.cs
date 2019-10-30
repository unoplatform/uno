#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreWindow : global::Windows.UI.Core.ICoreWindow,global::Windows.UI.Core.ICorePointerRedirector
	{
		#if false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Point PointerPosition
		{
			get
			{
				throw new global::System.NotImplementedException("The member Point CoreWindow.PointerPosition is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "Point CoreWindow.PointerPosition");
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Core.CoreCursor PointerCursor
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreCursor CoreWindow.PointerCursor is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "CoreCursor CoreWindow.PointerCursor");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsInputEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreWindow.IsInputEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "bool CoreWindow.IsInputEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Core.CoreWindowFlowDirection FlowDirection
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreWindowFlowDirection CoreWindow.FlowDirection is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "CoreWindowFlowDirection CoreWindow.FlowDirection");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  object AutomationHostProvider
		{
			get
			{
				throw new global::System.NotImplementedException("The member object CoreWindow.AutomationHostProvider is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Rect Bounds
		{
			get
			{
				throw new global::System.NotImplementedException("The member Rect CoreWindow.Bounds is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Collections.IPropertySet CustomProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IPropertySet CoreWindow.CustomProperties is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property Dispatcher
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool Visible
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreWindow.Visible is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Core.CoreWindowActivationMode ActivationMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreWindowActivationMode CoreWindow.ActivationMode is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.System.DispatcherQueue DispatcherQueue
		{
			get
			{
				throw new global::System.NotImplementedException("The member DispatcherQueue CoreWindow.DispatcherQueue is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Core.CoreWindow.AutomationHostProvider.get
		// Forced skipping of method Windows.UI.Core.CoreWindow.Bounds.get
		// Forced skipping of method Windows.UI.Core.CoreWindow.CustomProperties.get
		// Forced skipping of method Windows.UI.Core.CoreWindow.Dispatcher.get
		// Forced skipping of method Windows.UI.Core.CoreWindow.FlowDirection.get
		// Forced skipping of method Windows.UI.Core.CoreWindow.FlowDirection.set
		// Forced skipping of method Windows.UI.Core.CoreWindow.IsInputEnabled.get
		// Forced skipping of method Windows.UI.Core.CoreWindow.IsInputEnabled.set
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerCursor.get
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerCursor.set
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerPosition.get
		// Forced skipping of method Windows.UI.Core.CoreWindow.Visible.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Activate()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "void CoreWindow.Activate()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Close()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "void CoreWindow.Close()");
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Core.CoreVirtualKeyStates GetAsyncKeyState( global::Windows.System.VirtualKey virtualKey)
		{
			throw new global::System.NotImplementedException("The member CoreVirtualKeyStates CoreWindow.GetAsyncKeyState(VirtualKey virtualKey) is not implemented in Uno.");
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Core.CoreVirtualKeyStates GetKeyState( global::Windows.System.VirtualKey virtualKey)
		{
			throw new global::System.NotImplementedException("The member CoreVirtualKeyStates CoreWindow.GetKeyState(VirtualKey virtualKey) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void ReleasePointerCapture()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "void CoreWindow.ReleasePointerCapture()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void SetPointerCapture()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "void CoreWindow.SetPointerCapture()");
		}
		#endif
		// Forced skipping of method Windows.UI.Core.CoreWindow.Activated.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.Activated.remove
		// Forced skipping of method Windows.UI.Core.CoreWindow.AutomationProviderRequested.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.AutomationProviderRequested.remove
		// Forced skipping of method Windows.UI.Core.CoreWindow.CharacterReceived.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.CharacterReceived.remove
		// Forced skipping of method Windows.UI.Core.CoreWindow.Closed.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.Closed.remove
		// Forced skipping of method Windows.UI.Core.CoreWindow.InputEnabled.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.InputEnabled.remove
		// Forced skipping of method Windows.UI.Core.CoreWindow.KeyDown.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.KeyDown.remove
		// Forced skipping of method Windows.UI.Core.CoreWindow.KeyUp.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.KeyUp.remove
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerCaptureLost.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerCaptureLost.remove
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerEntered.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerEntered.remove
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerExited.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerExited.remove
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerMoved.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerMoved.remove
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerPressed.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerPressed.remove
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerReleased.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerReleased.remove
		// Forced skipping of method Windows.UI.Core.CoreWindow.TouchHitTesting.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.TouchHitTesting.remove
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerWheelChanged.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerWheelChanged.remove
		// Forced skipping of method Windows.UI.Core.CoreWindow.SizeChanged.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.SizeChanged.remove
		// Forced skipping of method Windows.UI.Core.CoreWindow.VisibilityChanged.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.VisibilityChanged.remove
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerPosition.set
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerRoutedAway.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerRoutedAway.remove
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerRoutedTo.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerRoutedTo.remove
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerRoutedReleased.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.PointerRoutedReleased.remove
		// Forced skipping of method Windows.UI.Core.CoreWindow.ClosestInteractiveBoundsRequested.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.ClosestInteractiveBoundsRequested.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string GetCurrentKeyEventDeviceId()
		{
			throw new global::System.NotImplementedException("The member string CoreWindow.GetCurrentKeyEventDeviceId() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Core.CoreWindow.ResizeStarted.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.ResizeStarted.remove
		// Forced skipping of method Windows.UI.Core.CoreWindow.ResizeCompleted.add
		// Forced skipping of method Windows.UI.Core.CoreWindow.ResizeCompleted.remove
		// Forced skipping of method Windows.UI.Core.CoreWindow.DispatcherQueue.get
		// Forced skipping of method Windows.UI.Core.CoreWindow.ActivationMode.get
		#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Core.CoreWindow GetForCurrentThread()
		{
			throw new global::System.NotImplementedException("The member CoreWindow CoreWindow.GetForCurrentThread() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreWindow, global::Windows.UI.Core.WindowActivatedEventArgs> Activated
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, WindowActivatedEventArgs> CoreWindow.Activated");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, WindowActivatedEventArgs> CoreWindow.Activated");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreWindow, global::Windows.UI.Core.AutomationProviderRequestedEventArgs> AutomationProviderRequested
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, AutomationProviderRequestedEventArgs> CoreWindow.AutomationProviderRequested");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, AutomationProviderRequestedEventArgs> CoreWindow.AutomationProviderRequested");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreWindow, global::Windows.UI.Core.CharacterReceivedEventArgs> CharacterReceived
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, CharacterReceivedEventArgs> CoreWindow.CharacterReceived");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, CharacterReceivedEventArgs> CoreWindow.CharacterReceived");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreWindow, global::Windows.UI.Core.CoreWindowEventArgs> Closed
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, CoreWindowEventArgs> CoreWindow.Closed");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, CoreWindowEventArgs> CoreWindow.Closed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreWindow, global::Windows.UI.Core.InputEnabledEventArgs> InputEnabled
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, InputEnabledEventArgs> CoreWindow.InputEnabled");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, InputEnabledEventArgs> CoreWindow.InputEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreWindow, global::Windows.UI.Core.KeyEventArgs> KeyDown
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, KeyEventArgs> CoreWindow.KeyDown");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, KeyEventArgs> CoreWindow.KeyDown");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreWindow, global::Windows.UI.Core.KeyEventArgs> KeyUp
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, KeyEventArgs> CoreWindow.KeyUp");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, KeyEventArgs> CoreWindow.KeyUp");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreWindow, global::Windows.UI.Core.PointerEventArgs> PointerCaptureLost
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, PointerEventArgs> CoreWindow.PointerCaptureLost");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, PointerEventArgs> CoreWindow.PointerCaptureLost");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreWindow, global::Windows.UI.Core.PointerEventArgs> PointerEntered
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, PointerEventArgs> CoreWindow.PointerEntered");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, PointerEventArgs> CoreWindow.PointerEntered");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreWindow, global::Windows.UI.Core.PointerEventArgs> PointerExited
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, PointerEventArgs> CoreWindow.PointerExited");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, PointerEventArgs> CoreWindow.PointerExited");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreWindow, global::Windows.UI.Core.PointerEventArgs> PointerMoved
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, PointerEventArgs> CoreWindow.PointerMoved");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, PointerEventArgs> CoreWindow.PointerMoved");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreWindow, global::Windows.UI.Core.PointerEventArgs> PointerPressed
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, PointerEventArgs> CoreWindow.PointerPressed");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, PointerEventArgs> CoreWindow.PointerPressed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreWindow, global::Windows.UI.Core.PointerEventArgs> PointerReleased
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, PointerEventArgs> CoreWindow.PointerReleased");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, PointerEventArgs> CoreWindow.PointerReleased");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreWindow, global::Windows.UI.Core.PointerEventArgs> PointerWheelChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, PointerEventArgs> CoreWindow.PointerWheelChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, PointerEventArgs> CoreWindow.PointerWheelChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreWindow, global::Windows.UI.Core.WindowSizeChangedEventArgs> SizeChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, WindowSizeChangedEventArgs> CoreWindow.SizeChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, WindowSizeChangedEventArgs> CoreWindow.SizeChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreWindow, global::Windows.UI.Core.TouchHitTestingEventArgs> TouchHitTesting
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, TouchHitTestingEventArgs> CoreWindow.TouchHitTesting");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, TouchHitTestingEventArgs> CoreWindow.TouchHitTesting");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreWindow, global::Windows.UI.Core.VisibilityChangedEventArgs> VisibilityChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, VisibilityChangedEventArgs> CoreWindow.VisibilityChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, VisibilityChangedEventArgs> CoreWindow.VisibilityChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.ICorePointerRedirector, global::Windows.UI.Core.PointerEventArgs> PointerRoutedAway
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<ICorePointerRedirector, PointerEventArgs> CoreWindow.PointerRoutedAway");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<ICorePointerRedirector, PointerEventArgs> CoreWindow.PointerRoutedAway");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.ICorePointerRedirector, global::Windows.UI.Core.PointerEventArgs> PointerRoutedReleased
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<ICorePointerRedirector, PointerEventArgs> CoreWindow.PointerRoutedReleased");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<ICorePointerRedirector, PointerEventArgs> CoreWindow.PointerRoutedReleased");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.ICorePointerRedirector, global::Windows.UI.Core.PointerEventArgs> PointerRoutedTo
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<ICorePointerRedirector, PointerEventArgs> CoreWindow.PointerRoutedTo");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<ICorePointerRedirector, PointerEventArgs> CoreWindow.PointerRoutedTo");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreWindow, global::Windows.UI.Core.ClosestInteractiveBoundsRequestedEventArgs> ClosestInteractiveBoundsRequested
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, ClosestInteractiveBoundsRequestedEventArgs> CoreWindow.ClosestInteractiveBoundsRequested");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, ClosestInteractiveBoundsRequestedEventArgs> CoreWindow.ClosestInteractiveBoundsRequested");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreWindow, object> ResizeCompleted
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, object> CoreWindow.ResizeCompleted");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, object> CoreWindow.ResizeCompleted");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreWindow, object> ResizeStarted
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, object> CoreWindow.ResizeStarted");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindow", "event TypedEventHandler<CoreWindow, object> CoreWindow.ResizeStarted");
			}
		}
		#endif
		// Processing: Windows.UI.Core.ICoreWindow
		// Processing: Windows.UI.Core.ICorePointerRedirector
	}
}
