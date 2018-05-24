#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class FlyoutBase : global::Windows.UI.Xaml.DependencyObject
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode Placement
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode)this.GetValue(PlacementProperty);
			}
			set
			{
				this.SetValue(PlacementProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.LightDismissOverlayMode LightDismissOverlayMode
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.LightDismissOverlayMode)this.GetValue(LightDismissOverlayModeProperty);
			}
			set
			{
				this.SetValue(LightDismissOverlayModeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
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
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
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
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
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
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.FrameworkElement Target
		{
			get
			{
				throw new global::System.NotImplementedException("The member FrameworkElement FlyoutBase.Target is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
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
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AttachedFlyoutProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"AttachedFlyout", typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PlacementProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Placement", typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AllowFocusOnInteractionProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"AllowFocusOnInteraction", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AllowFocusWhenDisabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"AllowFocusWhenDisabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ElementSoundModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ElementSoundMode", typeof(global::Windows.UI.Xaml.ElementSoundMode), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.ElementSoundMode)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty LightDismissOverlayModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"LightDismissOverlayMode", typeof(global::Windows.UI.Xaml.Controls.LightDismissOverlayMode), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.LightDismissOverlayMode)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty OverlayInputPassThroughElementProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"OverlayInputPassThroughElement", typeof(global::Windows.UI.Xaml.DependencyObject), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.DependencyObject)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected FlyoutBase() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.FlyoutBase", "FlyoutBase.FlyoutBase()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.FlyoutBase()
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Placement.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Placement.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Opened.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Opened.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Closed.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Closed.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Opening.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.Opening.remove
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  void ShowAt( global::Windows.UI.Xaml.FrameworkElement placementTarget)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.FlyoutBase", "void FlyoutBase.ShowAt(FrameworkElement placementTarget)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  void Hide()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.FlyoutBase", "void FlyoutBase.Hide()");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected virtual global::Windows.UI.Xaml.Controls.Control CreatePresenter()
		{
			throw new global::System.NotImplementedException("The member Control FlyoutBase.CreatePresenter() is not implemented in Uno.");
		}
		#endif
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
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void TryInvokeKeyboardAccelerator( global::Windows.UI.Xaml.Input.ProcessKeyboardAcceleratorEventArgs args)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.FlyoutBase", "void FlyoutBase.TryInvokeKeyboardAccelerator(ProcessKeyboardAcceleratorEventArgs args)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		protected virtual void OnProcessKeyboardAccelerators( global::Windows.UI.Xaml.Input.ProcessKeyboardAcceleratorEventArgs args)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.FlyoutBase", "void FlyoutBase.OnProcessKeyboardAccelerators(ProcessKeyboardAcceleratorEventArgs args)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.OverlayInputPassThroughElementProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.AllowFocusOnInteractionProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.LightDismissOverlayModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.AllowFocusWhenDisabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.ElementSoundModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.PlacementProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.FlyoutBase.AttachedFlyoutProperty.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase GetAttachedFlyout( global::Windows.UI.Xaml.FrameworkElement element)
		{
			return (global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase)element.GetValue(AttachedFlyoutProperty);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static void SetAttachedFlyout( global::Windows.UI.Xaml.FrameworkElement element,  global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase value)
		{
			element.SetValue(AttachedFlyoutProperty, value);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static void ShowAttachedFlyout( global::Windows.UI.Xaml.FrameworkElement flyoutOwner)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.FlyoutBase", "void FlyoutBase.ShowAttachedFlyout(FrameworkElement flyoutOwner)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::System.EventHandler<object> Closed
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.FlyoutBase", "event EventHandler<object> FlyoutBase.Closed");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.FlyoutBase", "event EventHandler<object> FlyoutBase.Closed");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::System.EventHandler<object> Opened
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.FlyoutBase", "event EventHandler<object> FlyoutBase.Opened");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.FlyoutBase", "event EventHandler<object> FlyoutBase.Opened");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::System.EventHandler<object> Opening
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.FlyoutBase", "event EventHandler<object> FlyoutBase.Opening");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.FlyoutBase", "event EventHandler<object> FlyoutBase.Opening");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase, global::Windows.UI.Xaml.Controls.Primitives.FlyoutBaseClosingEventArgs> Closing
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.FlyoutBase", "event TypedEventHandler<FlyoutBase, FlyoutBaseClosingEventArgs> FlyoutBase.Closing");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.FlyoutBase", "event TypedEventHandler<FlyoutBase, FlyoutBaseClosingEventArgs> FlyoutBase.Closing");
			}
		}
		#endif
	}
}
