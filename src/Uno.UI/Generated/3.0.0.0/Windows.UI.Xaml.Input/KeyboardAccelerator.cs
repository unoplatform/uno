#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class KeyboardAccelerator : global::Windows.UI.Xaml.DependencyObject
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.DependencyObject ScopeOwner
		{
			get
			{
				return (global::Windows.UI.Xaml.DependencyObject)this.GetValue(ScopeOwnerProperty);
			}
			set
			{
				this.SetValue(ScopeOwnerProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.VirtualKeyModifiers Modifiers
		{
			get
			{
				return (global::Windows.System.VirtualKeyModifiers)this.GetValue(ModifiersProperty);
			}
			set
			{
				this.SetValue(ModifiersProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.VirtualKey Key
		{
			get
			{
				return (global::Windows.System.VirtualKey)this.GetValue(KeyProperty);
			}
			set
			{
				this.SetValue(KeyProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsEnabled
		{
			get
			{
				return (bool)this.GetValue(IsEnabledProperty);
			}
			set
			{
				this.SetValue(IsEnabledProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty IsEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(IsEnabled), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Input.KeyboardAccelerator), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty KeyProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Key), typeof(global::Windows.System.VirtualKey), 
			typeof(global::Windows.UI.Xaml.Input.KeyboardAccelerator), 
			new FrameworkPropertyMetadata(default(global::Windows.System.VirtualKey)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty ModifiersProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Modifiers), typeof(global::Windows.System.VirtualKeyModifiers), 
			typeof(global::Windows.UI.Xaml.Input.KeyboardAccelerator), 
			new FrameworkPropertyMetadata(default(global::Windows.System.VirtualKeyModifiers)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty ScopeOwnerProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(ScopeOwner), typeof(global::Windows.UI.Xaml.DependencyObject), 
			typeof(global::Windows.UI.Xaml.Input.KeyboardAccelerator), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.DependencyObject)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Input.KeyboardAccelerator.KeyboardAccelerator()
		// Forced skipping of method Windows.UI.Xaml.Input.KeyboardAccelerator.KeyboardAccelerator()
		// Forced skipping of method Windows.UI.Xaml.Input.KeyboardAccelerator.Key.get
		// Forced skipping of method Windows.UI.Xaml.Input.KeyboardAccelerator.Key.set
		// Forced skipping of method Windows.UI.Xaml.Input.KeyboardAccelerator.Modifiers.get
		// Forced skipping of method Windows.UI.Xaml.Input.KeyboardAccelerator.Modifiers.set
		// Forced skipping of method Windows.UI.Xaml.Input.KeyboardAccelerator.IsEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Input.KeyboardAccelerator.IsEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Input.KeyboardAccelerator.ScopeOwner.get
		// Forced skipping of method Windows.UI.Xaml.Input.KeyboardAccelerator.ScopeOwner.set
		// Forced skipping of method Windows.UI.Xaml.Input.KeyboardAccelerator.Invoked.add
		// Forced skipping of method Windows.UI.Xaml.Input.KeyboardAccelerator.Invoked.remove
		// Forced skipping of method Windows.UI.Xaml.Input.KeyboardAccelerator.KeyProperty.get
		// Forced skipping of method Windows.UI.Xaml.Input.KeyboardAccelerator.ModifiersProperty.get
		// Forced skipping of method Windows.UI.Xaml.Input.KeyboardAccelerator.IsEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Input.KeyboardAccelerator.ScopeOwnerProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Input.KeyboardAccelerator, global::Windows.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs> Invoked
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.KeyboardAccelerator", "event TypedEventHandler<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs> KeyboardAccelerator.Invoked");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.KeyboardAccelerator", "event TypedEventHandler<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs> KeyboardAccelerator.Invoked");
			}
		}
		#endif
	}
}
