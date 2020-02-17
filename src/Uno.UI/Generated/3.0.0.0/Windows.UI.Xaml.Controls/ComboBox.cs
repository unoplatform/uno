#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ComboBox 
	{
		// Skipping already declared property MaxDropDownHeight
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsEditable
		{
			get
			{
				return (bool)this.GetValue(IsEditableProperty);
			}
			set
			{
				this.SetValue(IsEditableProperty, value);
			}
		}
		#endif
		// Skipping already declared property IsDropDownOpen
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsSelectionBoxHighlighted
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ComboBox.IsSelectionBoxHighlighted is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  object SelectionBoxItem
		{
			get
			{
				throw new global::System.NotImplementedException("The member object ComboBox.SelectionBoxItem is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.DataTemplate SelectionBoxItemTemplate
		{
			get
			{
				throw new global::System.NotImplementedException("The member DataTemplate ComboBox.SelectionBoxItemTemplate is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property TemplateSettings
		// Skipping already declared property PlaceholderText
		// Skipping already declared property HeaderTemplate
		// Skipping already declared property Header
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Style TextBoxStyle
		{
			get
			{
				return (global::Windows.UI.Xaml.Style)this.GetValue(TextBoxStyleProperty);
			}
			set
			{
				this.SetValue(TextBoxStyleProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Text
		{
			get
			{
				return (string)this.GetValue(TextProperty);
			}
			set
			{
				this.SetValue(TextProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  object Description
		{
			get
			{
				return (object)this.GetValue(DescriptionProperty);
			}
			set
			{
				this.SetValue(DescriptionProperty, value);
			}
		}
		#endif
		// Skipping already declared property IsDropDownOpenProperty
		// Skipping already declared property MaxDropDownHeightProperty
		// Skipping already declared property HeaderProperty
		// Skipping already declared property HeaderTemplateProperty
		// Skipping already declared property PlaceholderTextProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsTextSearchEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsTextSearchEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ComboBox), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SelectionChangedTriggerProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SelectionChangedTrigger", typeof(global::Windows.UI.Xaml.Controls.ComboBoxSelectionChangedTrigger), 
			typeof(global::Windows.UI.Xaml.Controls.ComboBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.ComboBoxSelectionChangedTrigger)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PlaceholderForegroundProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PlaceholderForeground", typeof(global::Windows.UI.Xaml.Media.Brush), 
			typeof(global::Windows.UI.Xaml.Controls.ComboBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DescriptionProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Description", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.ComboBox), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsEditableProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsEditable", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ComboBox), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TextBoxStyleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TextBoxStyle", typeof(global::Windows.UI.Xaml.Style), 
			typeof(global::Windows.UI.Xaml.Controls.ComboBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Style)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TextProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Text", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.ComboBox), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.ComboBox.ComboBox()
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
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.IsEditable.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.Text.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.Text.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.TextBoxStyle.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.TextBoxStyle.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.Description.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.Description.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.TextSubmitted.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.TextSubmitted.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		protected virtual void OnDropDownClosed( object e)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ComboBox", "void ComboBox.OnDropDownClosed(object e)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		protected virtual void OnDropDownOpened( object e)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ComboBox", "void ComboBox.OnDropDownOpened(object e)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.IsEditableProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.TextProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.TextBoxStyleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.DescriptionProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.PlaceholderForegroundProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.SelectionChangedTriggerProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.LightDismissOverlayModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.IsTextSearchEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.HeaderProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.HeaderTemplateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.PlaceholderTextProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.IsDropDownOpenProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ComboBox.MaxDropDownHeightProperty.get
		// Skipping already declared event Windows.UI.Xaml.Controls.ComboBox.DropDownClosed
		// Skipping already declared event Windows.UI.Xaml.Controls.ComboBox.DropDownOpened
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.ComboBox, global::Windows.UI.Xaml.Controls.ComboBoxTextSubmittedEventArgs> TextSubmitted
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ComboBox", "event TypedEventHandler<ComboBox, ComboBoxTextSubmittedEventArgs> ComboBox.TextSubmitted");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ComboBox", "event TypedEventHandler<ComboBox, ComboBoxTextSubmittedEventArgs> ComboBox.TextSubmitted");
			}
		}
		#endif
	}
}
