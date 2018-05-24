#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ComboBox 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double MaxDropDownHeight
		{
			get
			{
				return (double)this.GetValue(MaxDropDownHeightProperty);
			}
			set
			{
				this.SetValue(MaxDropDownHeightProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsDropDownOpen
		{
			get
			{
				return (bool)this.GetValue(IsDropDownOpenProperty);
			}
			set
			{
				this.SetValue(IsDropDownOpenProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool IsSelectionBoxHighlighted
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ComboBox.IsSelectionBoxHighlighted is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool IsEditable
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ComboBox.IsEditable is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  object SelectionBoxItem
		{
			get
			{
				throw new global::System.NotImplementedException("The member object ComboBox.SelectionBoxItem is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.DataTemplate SelectionBoxItemTemplate
		{
			get
			{
				throw new global::System.NotImplementedException("The member DataTemplate ComboBox.SelectionBoxItemTemplate is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Primitives.ComboBoxTemplateSettings TemplateSettings
		{
			get
			{
				throw new global::System.NotImplementedException("The member ComboBoxTemplateSettings ComboBox.TemplateSettings is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.DataTemplate HeaderTemplate
		{
			get
			{
				return (global::Windows.UI.Xaml.DataTemplate)this.GetValue(HeaderTemplateProperty);
			}
			set
			{
				this.SetValue(HeaderTemplateProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  string PlaceholderText
		{
			get
			{
				return (string)this.GetValue(PlaceholderTextProperty);
			}
			set
			{
				this.SetValue(PlaceholderTextProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  object Header
		{
			get
			{
				return (object)this.GetValue(HeaderProperty);
			}
			set
			{
				this.SetValue(HeaderProperty, value);
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
		public  bool IsTextSearchEnabled
		{
			get
			{
				return (bool)this.GetValue(IsTextSearchEnabledProperty);
			}
			set
			{
				this.SetValue(IsTextSearchEnabledProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.ComboBoxSelectionChangedTrigger SelectionChangedTrigger
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.ComboBoxSelectionChangedTrigger)this.GetValue(SelectionChangedTriggerProperty);
			}
			set
			{
				this.SetValue(SelectionChangedTriggerProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Brush PlaceholderForeground
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(PlaceholderForegroundProperty);
			}
			set
			{
				this.SetValue(PlaceholderForegroundProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsDropDownOpenProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsDropDownOpen", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ComboBox), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MaxDropDownHeightProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MaxDropDownHeight", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.ComboBox), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HeaderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Header", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.ComboBox), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HeaderTemplateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"HeaderTemplate", typeof(global::Windows.UI.Xaml.DataTemplate), 
			typeof(global::Windows.UI.Xaml.Controls.ComboBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.DataTemplate)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PlaceholderTextProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PlaceholderText", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.ComboBox), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsTextSearchEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsTextSearchEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ComboBox), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty LightDismissOverlayModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"LightDismissOverlayMode", typeof(global::Windows.UI.Xaml.Controls.LightDismissOverlayMode), 
			typeof(global::Windows.UI.Xaml.Controls.ComboBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.LightDismissOverlayMode)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SelectionChangedTriggerProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SelectionChangedTrigger", typeof(global::Windows.UI.Xaml.Controls.ComboBoxSelectionChangedTrigger), 
			typeof(global::Windows.UI.Xaml.Controls.ComboBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.ComboBoxSelectionChangedTrigger)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PlaceholderForegroundProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PlaceholderForeground", typeof(global::Windows.UI.Xaml.Media.Brush), 
			typeof(global::Windows.UI.Xaml.Controls.ComboBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public ComboBox() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ComboBox", "ComboBox.ComboBox()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.ComboBox()
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.IsDropDownOpen.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.IsDropDownOpen.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.IsEditable.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.IsSelectionBoxHighlighted.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.MaxDropDownHeight.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.MaxDropDownHeight.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.SelectionBoxItem.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.SelectionBoxItemTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.TemplateSettings.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.DropDownClosed.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.DropDownClosed.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.DropDownOpened.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.DropDownOpened.remove
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		protected virtual void OnDropDownClosed( object e)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ComboBox", "void ComboBox.OnDropDownClosed(object e)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		protected virtual void OnDropDownOpened( object e)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ComboBox", "void ComboBox.OnDropDownOpened(object e)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.Header.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.Header.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.HeaderTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.HeaderTemplate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.PlaceholderText.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.PlaceholderText.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.LightDismissOverlayMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.LightDismissOverlayMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.IsTextSearchEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.IsTextSearchEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.SelectionChangedTrigger.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.SelectionChangedTrigger.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.PlaceholderForeground.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.PlaceholderForeground.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.PlaceholderForegroundProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.SelectionChangedTriggerProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.LightDismissOverlayModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.IsTextSearchEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.HeaderProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.HeaderTemplateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.PlaceholderTextProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.IsDropDownOpenProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.MaxDropDownHeightProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::System.EventHandler<object> DropDownClosed
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ComboBox", "event EventHandler<object> ComboBox.DropDownClosed");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ComboBox", "event EventHandler<object> ComboBox.DropDownClosed");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::System.EventHandler<object> DropDownOpened
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ComboBox", "event EventHandler<object> ComboBox.DropDownOpened");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ComboBox", "event EventHandler<object> ComboBox.DropDownOpened");
			}
		}
		#endif
	}
}
