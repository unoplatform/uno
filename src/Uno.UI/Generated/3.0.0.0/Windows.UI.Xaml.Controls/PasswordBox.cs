#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class PasswordBox 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  string PasswordChar
		{
			get
			{
				return (string)this.GetValue(PasswordCharProperty);
			}
			set
			{
				this.SetValue(PasswordCharProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  string Password
		{
			get
			{
				return (string)this.GetValue(PasswordProperty);
			}
			set
			{
				this.SetValue(PasswordProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  int MaxLength
		{
			get
			{
				return (int)this.GetValue(MaxLengthProperty);
			}
			set
			{
				this.SetValue(MaxLengthProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool IsPasswordRevealButtonEnabled
		{
			get
			{
				return (bool)this.GetValue(IsPasswordRevealButtonEnabledProperty);
			}
			set
			{
				this.SetValue(IsPasswordRevealButtonEnabledProperty, value);
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
		public  global::Windows.UI.Xaml.Media.SolidColorBrush SelectionHighlightColor
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.SolidColorBrush)this.GetValue(SelectionHighlightColorProperty);
			}
			set
			{
				this.SetValue(SelectionHighlightColorProperty, value);
			}
		}
		#endif
		#if false || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool PreventKeyboardDisplayOnProgrammaticFocus
		{
			get
			{
				return (bool)this.GetValue(PreventKeyboardDisplayOnProgrammaticFocusProperty);
			}
			set
			{
				this.SetValue(PreventKeyboardDisplayOnProgrammaticFocusProperty, value);
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
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.TextReadingOrder TextReadingOrder
		{
			get
			{
				return (global::Windows.UI.Xaml.TextReadingOrder)this.GetValue(TextReadingOrderProperty);
			}
			set
			{
				this.SetValue(TextReadingOrderProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.PasswordRevealMode PasswordRevealMode
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.PasswordRevealMode)this.GetValue(PasswordRevealModeProperty);
			}
			set
			{
				this.SetValue(PasswordRevealModeProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Input.InputScope InputScope
		{
			get
			{
				return (global::Windows.UI.Xaml.Input.InputScope)this.GetValue(InputScopeProperty);
			}
			set
			{
				this.SetValue(InputScopeProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PasswordProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Password", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.PasswordBox), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsPasswordRevealButtonEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsPasswordRevealButtonEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.PasswordBox), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MaxLengthProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MaxLength", typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.PasswordBox), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PasswordCharProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PasswordChar", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.PasswordBox), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HeaderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Header", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.PasswordBox), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HeaderTemplateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"HeaderTemplate", typeof(global::Windows.UI.Xaml.DataTemplate), 
			typeof(global::Windows.UI.Xaml.Controls.PasswordBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.DataTemplate)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PlaceholderTextProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PlaceholderText", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.PasswordBox), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if false || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PreventKeyboardDisplayOnProgrammaticFocusProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PreventKeyboardDisplayOnProgrammaticFocus", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.PasswordBox), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SelectionHighlightColorProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SelectionHighlightColor", typeof(global::Windows.UI.Xaml.Media.SolidColorBrush), 
			typeof(global::Windows.UI.Xaml.Controls.PasswordBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.SolidColorBrush)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TextReadingOrderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TextReadingOrder", typeof(global::Windows.UI.Xaml.TextReadingOrder), 
			typeof(global::Windows.UI.Xaml.Controls.PasswordBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.TextReadingOrder)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PasswordRevealModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PasswordRevealMode", typeof(global::Windows.UI.Xaml.Controls.PasswordRevealMode), 
			typeof(global::Windows.UI.Xaml.Controls.PasswordBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.PasswordRevealMode)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty InputScopeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"InputScope", typeof(global::Windows.UI.Xaml.Input.InputScope), 
			typeof(global::Windows.UI.Xaml.Controls.PasswordBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Input.InputScope)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public PasswordBox() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PasswordBox", "PasswordBox.PasswordBox()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.PasswordBox()
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.Password.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.Password.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.PasswordChar.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.PasswordChar.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.IsPasswordRevealButtonEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.IsPasswordRevealButtonEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.MaxLength.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.MaxLength.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.PasswordChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.PasswordChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.ContextMenuOpening.add
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.ContextMenuOpening.remove
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void SelectAll()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PasswordBox", "void PasswordBox.SelectAll()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.Header.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.Header.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.HeaderTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.HeaderTemplate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.PlaceholderText.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.PlaceholderText.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.SelectionHighlightColor.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.SelectionHighlightColor.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.PreventKeyboardDisplayOnProgrammaticFocus.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.PreventKeyboardDisplayOnProgrammaticFocus.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.Paste.add
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.Paste.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.PasswordRevealMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.PasswordRevealMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.TextReadingOrder.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.TextReadingOrder.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.InputScope.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.InputScope.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.PasswordChanging.add
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.PasswordChanging.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.PasswordRevealModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.TextReadingOrderProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.InputScopeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.HeaderProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.HeaderTemplateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.PlaceholderTextProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.SelectionHighlightColorProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.PreventKeyboardDisplayOnProgrammaticFocusProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.PasswordProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.PasswordCharProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.IsPasswordRevealButtonEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.MaxLengthProperty.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.Controls.ContextMenuOpeningEventHandler ContextMenuOpening
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PasswordBox", "event ContextMenuOpeningEventHandler PasswordBox.ContextMenuOpening");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PasswordBox", "event ContextMenuOpeningEventHandler PasswordBox.ContextMenuOpening");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.RoutedEventHandler PasswordChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PasswordBox", "event RoutedEventHandler PasswordBox.PasswordChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PasswordBox", "event RoutedEventHandler PasswordBox.PasswordChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.Controls.TextControlPasteEventHandler Paste
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PasswordBox", "event TextControlPasteEventHandler PasswordBox.Paste");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PasswordBox", "event TextControlPasteEventHandler PasswordBox.Paste");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.PasswordBox, global::Windows.UI.Xaml.Controls.PasswordBoxPasswordChangingEventArgs> PasswordChanging
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PasswordBox", "event TypedEventHandler<PasswordBox, PasswordBoxPasswordChangingEventArgs> PasswordBox.PasswordChanging");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PasswordBox", "event TypedEventHandler<PasswordBox, PasswordBoxPasswordChangingEventArgs> PasswordBox.PasswordChanging");
			}
		}
		#endif
	}
}
