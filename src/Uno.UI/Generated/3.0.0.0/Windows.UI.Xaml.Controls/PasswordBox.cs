#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class PasswordBox 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
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
		// Skipping already declared property Password
		// Skipping already declared property MaxLength
		// Skipping already declared property IsPasswordRevealButtonEnabled
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
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
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
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
		// Skipping already declared property PlaceholderText
		// Skipping already declared property HeaderTemplate
		// Skipping already declared property Header
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
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
		// Skipping already declared property InputScope
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase SelectionFlyout
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase)this.GetValue(SelectionFlyoutProperty);
			}
			set
			{
				this.SetValue(SelectionFlyoutProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanPasteClipboardContent
		{
			get
			{
				return (bool)this.GetValue(CanPasteClipboardContentProperty);
			}
		}
		#endif
		// Skipping already declared property IsPasswordRevealButtonEnabledProperty
		// Skipping already declared property MaxLengthProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty PasswordCharProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(PasswordChar), typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.PasswordBox), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		// Skipping already declared property PasswordProperty
		// Skipping already declared property HeaderProperty
		// Skipping already declared property HeaderTemplateProperty
		// Skipping already declared property PlaceholderTextProperty
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty PreventKeyboardDisplayOnProgrammaticFocusProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(PreventKeyboardDisplayOnProgrammaticFocus), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.PasswordBox), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty SelectionHighlightColorProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(SelectionHighlightColor), typeof(global::Windows.UI.Xaml.Media.SolidColorBrush), 
			typeof(global::Windows.UI.Xaml.Controls.PasswordBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.SolidColorBrush)));
		#endif
		// Skipping already declared property InputScopeProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty PasswordRevealModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(PasswordRevealMode), typeof(global::Windows.UI.Xaml.Controls.PasswordRevealMode), 
			typeof(global::Windows.UI.Xaml.Controls.PasswordBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.PasswordRevealMode)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty TextReadingOrderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(TextReadingOrder), typeof(global::Windows.UI.Xaml.TextReadingOrder), 
			typeof(global::Windows.UI.Xaml.Controls.PasswordBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.TextReadingOrder)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty CanPasteClipboardContentProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(CanPasteClipboardContent), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.PasswordBox), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty DescriptionProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Description), typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.PasswordBox), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty SelectionFlyoutProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(SelectionFlyout), typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			typeof(global::Windows.UI.Xaml.Controls.PasswordBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.PasswordBox.PasswordBox()
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
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
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.CanPasteClipboardContent.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.SelectionFlyout.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.SelectionFlyout.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.Description.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.Description.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void PasteFromClipboard()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PasswordBox", "void PasswordBox.PasteFromClipboard()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.CanPasteClipboardContentProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.SelectionFlyoutProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PasswordBox.DescriptionProperty.get
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.UI.Xaml.Controls.ContextMenuOpeningEventHandler ContextMenuOpening
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PasswordBox", "event ContextMenuOpeningEventHandler PasswordBox.ContextMenuOpening");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PasswordBox", "event ContextMenuOpeningEventHandler PasswordBox.ContextMenuOpening");
			}
		}
		#endif
		// Skipping already declared event Windows.UI.Xaml.Controls.PasswordBox.PasswordChanged
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.UI.Xaml.Controls.TextControlPasteEventHandler Paste
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PasswordBox", "event TextControlPasteEventHandler PasswordBox.Paste");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PasswordBox", "event TextControlPasteEventHandler PasswordBox.Paste");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.PasswordBox, global::Windows.UI.Xaml.Controls.PasswordBoxPasswordChangingEventArgs> PasswordChanging
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PasswordBox", "event TypedEventHandler<PasswordBox, PasswordBoxPasswordChangingEventArgs> PasswordBox.PasswordChanging");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PasswordBox", "event TypedEventHandler<PasswordBox, PasswordBoxPasswordChangingEventArgs> PasswordBox.PasswordChanging");
			}
		}
		#endif
	}
}
