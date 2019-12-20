#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class FlyoutBase : global::Windows.UI.Xaml.DependencyObject
	{
		// Skipping already declared property Placement
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.ElementSoundMode ElementSoundMode
		{
			get
			{
				return (global::Windows.UI.Xaml.ElementSoundMode)this.GetValue(ElementSoundModeProperty);
			}
			set
			{
				this.SetValue(ElementSoundModeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool AllowFocusWhenDisabled
		{
			get
			{
				return (bool)this.GetValue(AllowFocusWhenDisabledProperty);
			}
			set
			{
				this.SetValue(AllowFocusWhenDisabledProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool AllowFocusOnInteraction
		{
			get
			{
				return (bool)this.GetValue(AllowFocusOnInteractionProperty);
			}
			set
			{
				this.SetValue(AllowFocusOnInteractionProperty, value);
			}
		}
		#endif
		// Skipping already declared property Target
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.DependencyObject OverlayInputPassThroughElement
		{
			get
			{
				return (global::Windows.UI.Xaml.DependencyObject)this.GetValue(OverlayInputPassThroughElementProperty);
			}
			set
			{
				this.SetValue(OverlayInputPassThroughElementProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Primitives.FlyoutShowMode ShowMode
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Primitives.FlyoutShowMode)this.GetValue(ShowModeProperty);
			}
			set
			{
				this.SetValue(ShowModeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool AreOpenCloseAnimationsEnabled
		{
			get
			{
				return (bool)this.GetValue(AreOpenCloseAnimationsEnabledProperty);
			}
			set
			{
				this.SetValue(AreOpenCloseAnimationsEnabledProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool InputDevicePrefersPrimaryCommands
		{
			get
			{
				return (bool)this.GetValue(InputDevicePrefersPrimaryCommandsProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsOpen
		{
			get
			{
				return (bool)this.GetValue(IsOpenProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AttachedFlyoutProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"AttachedFlyout", typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase)));
		#endif
		// Skipping already declared property PlacementProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AllowFocusOnInteractionProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"AllowFocusOnInteraction", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AllowFocusWhenDisabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"AllowFocusWhenDisabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ElementSoundModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ElementSoundMode", typeof(global::Windows.UI.Xaml.ElementSoundMode), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.ElementSoundMode)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty OverlayInputPassThroughElementProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"OverlayInputPassThroughElement", typeof(global::Windows.UI.Xaml.DependencyObject), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.DependencyObject)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AreOpenCloseAnimationsEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"AreOpenCloseAnimationsEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty InputDevicePrefersPrimaryCommandsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"InputDevicePrefersPrimaryCommands", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsOpenProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsOpen", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ShowModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ShowMode", typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutShowMode), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Primitives.FlyoutShowMode)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TargetProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Target", typeof(global::Windows.UI.Xaml.FrameworkElement), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.FrameworkElement)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.FlyoutBase()
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.FlyoutBase()
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Placement.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Placement.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Opened.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Opened.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Closed.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Closed.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Opening.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Opening.remove
		// Skipping already declared method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.ShowAt(Windows.UI.Xaml.FrameworkElement)
		// Skipping already declared method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Hide()
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Target.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.AllowFocusOnInteraction.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.AllowFocusOnInteraction.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.LightDismissOverlayMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.LightDismissOverlayMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.AllowFocusWhenDisabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.AllowFocusWhenDisabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.ElementSoundMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.ElementSoundMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Closing.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Closing.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.OverlayInputPassThroughElement.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.OverlayInputPassThroughElement.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void TryInvokeKeyboardAccelerator( global::Windows.UI.Xaml.Input.ProcessKeyboardAcceleratorEventArgs args)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.FlyoutBase", "void FlyoutBase.TryInvokeKeyboardAccelerator(ProcessKeyboardAcceleratorEventArgs args)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.ShowMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.ShowMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.InputDevicePrefersPrimaryCommands.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.AreOpenCloseAnimationsEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.AreOpenCloseAnimationsEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.IsOpen.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void ShowAt( global::Windows.UI.Xaml.DependencyObject placementTarget,  global::Windows.UI.Xaml.Controls.Primitives.FlyoutShowOptions showOptions)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.FlyoutBase", "void FlyoutBase.ShowAt(DependencyObject placementTarget, FlyoutShowOptions showOptions)");
		}
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.CreatePresenter()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		protected virtual void OnProcessKeyboardAccelerators( global::Windows.UI.Xaml.Input.ProcessKeyboardAcceleratorEventArgs args)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.FlyoutBase", "void FlyoutBase.OnProcessKeyboardAccelerators(ProcessKeyboardAcceleratorEventArgs args)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.TargetProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.ShowModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.InputDevicePrefersPrimaryCommandsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.AreOpenCloseAnimationsEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.IsOpenProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.OverlayInputPassThroughElementProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.AllowFocusOnInteractionProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.LightDismissOverlayModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.AllowFocusWhenDisabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.ElementSoundModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.PlacementProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.AttachedFlyoutProperty.get
		#if false || false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase GetAttachedFlyout( global::Windows.UI.Xaml.FrameworkElement element)
		{
			return (global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase)element.GetValue(AttachedFlyoutProperty);
		}
		#endif
		#if false || false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetAttachedFlyout( global::Windows.UI.Xaml.FrameworkElement element,  global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase value)
		{
			element.SetValue(AttachedFlyoutProperty, value);
		}
#endif
#if false || false || false || false || false
		[global::Uno.NotImplemented]
		public static void ShowAttachedFlyout( global::Windows.UI.Xaml.FrameworkElement flyoutOwner)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.FlyoutBase", "void FlyoutBase.ShowAttachedFlyout(FrameworkElement flyoutOwner)");
		}
#endif
		// Skipping already declared event Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Closed
		// Skipping already declared event Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Opened
		// Skipping already declared event Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Opening
		// Skipping already declared event Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Closing
	}
}
