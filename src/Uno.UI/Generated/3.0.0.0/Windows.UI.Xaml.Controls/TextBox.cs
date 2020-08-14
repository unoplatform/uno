#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class TextBox 
	{
		// Skipping already declared property TextWrapping
		// Skipping already declared property TextAlignment
		// Skipping already declared property Text
		#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__")]
		public  int SelectionStart
		{
			get
			{
				throw new global::System.NotImplementedException("The member int TextBox.SelectionStart is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "int TextBox.SelectionStart");
			}
		}
		#endif
		#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__")]
		public  int SelectionLength
		{
			get
			{
				throw new global::System.NotImplementedException("The member int TextBox.SelectionLength is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "int TextBox.SelectionLength");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string SelectedText
		{
			get
			{
				throw new global::System.NotImplementedException("The member string TextBox.SelectedText is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "string TextBox.SelectedText");
			}
		}
		#endif
		// Skipping already declared property MaxLength
		// Skipping already declared property IsTextPredictionEnabled
		// Skipping already declared property IsSpellCheckEnabled
		// Skipping already declared property IsReadOnly
		// Skipping already declared property InputScope
		// Skipping already declared property AcceptsReturn
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsColorFontEnabled
		{
			get
			{
				return (bool)this.GetValue(IsColorFontEnabledProperty);
			}
			set
			{
				this.SetValue(IsColorFontEnabledProperty, value);
			}
		}
		#endif
		// Skipping already declared property PlaceholderText
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
		// Skipping already declared property HeaderTemplate
		// Skipping already declared property Header
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.CandidateWindowAlignment DesiredCandidateWindowAlignment
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.CandidateWindowAlignment)this.GetValue(DesiredCandidateWindowAlignmentProperty);
			}
			set
			{
				this.SetValue(DesiredCandidateWindowAlignmentProperty, value);
			}
		}
		#endif
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
		public  global::Windows.UI.Xaml.Media.SolidColorBrush SelectionHighlightColorWhenNotFocused
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.SolidColorBrush)this.GetValue(SelectionHighlightColorWhenNotFocusedProperty);
			}
			set
			{
				this.SetValue(SelectionHighlightColorWhenNotFocusedProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.TextAlignment HorizontalTextAlignment
		{
			get
			{
				return (global::Windows.UI.Xaml.TextAlignment)this.GetValue(HorizontalTextAlignmentProperty);
			}
			set
			{
				this.SetValue(HorizontalTextAlignmentProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.CharacterCasing CharacterCasing
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.CharacterCasing)this.GetValue(CharacterCasingProperty);
			}
			set
			{
				this.SetValue(CharacterCasingProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsHandwritingViewEnabled
		{
			get
			{
				return (bool)this.GetValue(IsHandwritingViewEnabledProperty);
			}
			set
			{
				this.SetValue(IsHandwritingViewEnabledProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.HandwritingView HandwritingView
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.HandwritingView)this.GetValue(HandwritingViewProperty);
			}
			set
			{
				this.SetValue(HandwritingViewProperty, value);
			}
		}
		#endif
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanRedo
		{
			get
			{
				return (bool)this.GetValue(CanRedoProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanUndo
		{
			get
			{
				return (bool)this.GetValue(CanUndoProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase ProofingMenuFlyout
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase)this.GetValue(ProofingMenuFlyoutProperty);
			}
		}
		#endif
		// Skipping already declared property TextWrappingProperty
		// Skipping already declared property TextProperty
		// Skipping already declared property TextAlignmentProperty
		// Skipping already declared property MaxLengthProperty
		// Skipping already declared property IsTextPredictionEnabledProperty
		// Skipping already declared property IsSpellCheckEnabledProperty
		// Skipping already declared property IsReadOnlyProperty
		// Skipping already declared property InputScopeProperty
		// Skipping already declared property AcceptsReturnProperty
		// Skipping already declared property HeaderTemplateProperty
		// Skipping already declared property HeaderProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty SelectionHighlightColorProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(SelectionHighlightColor), typeof(global::Windows.UI.Xaml.Media.SolidColorBrush), 
			typeof(global::Windows.UI.Xaml.Controls.TextBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.SolidColorBrush)));
		#endif
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty PreventKeyboardDisplayOnProgrammaticFocusProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(PreventKeyboardDisplayOnProgrammaticFocus), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.TextBox), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		// Skipping already declared property PlaceholderTextProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty IsColorFontEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(IsColorFontEnabled), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.TextBox), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty DesiredCandidateWindowAlignmentProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(DesiredCandidateWindowAlignment), typeof(global::Windows.UI.Xaml.Controls.CandidateWindowAlignment), 
			typeof(global::Windows.UI.Xaml.Controls.TextBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.CandidateWindowAlignment)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty TextReadingOrderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(TextReadingOrder), typeof(global::Windows.UI.Xaml.TextReadingOrder), 
			typeof(global::Windows.UI.Xaml.Controls.TextBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.TextReadingOrder)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty SelectionHighlightColorWhenNotFocusedProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(SelectionHighlightColorWhenNotFocused), typeof(global::Windows.UI.Xaml.Media.SolidColorBrush), 
			typeof(global::Windows.UI.Xaml.Controls.TextBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.SolidColorBrush)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty PlaceholderForegroundProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(PlaceholderForeground), typeof(global::Windows.UI.Xaml.Media.Brush), 
			typeof(global::Windows.UI.Xaml.Controls.TextBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty HorizontalTextAlignmentProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(HorizontalTextAlignment), typeof(global::Windows.UI.Xaml.TextAlignment), 
			typeof(global::Windows.UI.Xaml.Controls.TextBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.TextAlignment)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty CharacterCasingProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(CharacterCasing), typeof(global::Windows.UI.Xaml.Controls.CharacterCasing), 
			typeof(global::Windows.UI.Xaml.Controls.TextBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.CharacterCasing)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty IsHandwritingViewEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(IsHandwritingViewEnabled), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.TextBox), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty HandwritingViewProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(HandwritingView), typeof(global::Windows.UI.Xaml.Controls.HandwritingView), 
			typeof(global::Windows.UI.Xaml.Controls.TextBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.HandwritingView)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty SelectionFlyoutProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(SelectionFlyout), typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			typeof(global::Windows.UI.Xaml.Controls.TextBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty ProofingMenuFlyoutProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(ProofingMenuFlyout), typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			typeof(global::Windows.UI.Xaml.Controls.TextBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty DescriptionProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Description), typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.TextBox), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty CanUndoProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(CanUndo), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.TextBox), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty CanRedoProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(CanRedo), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.TextBox), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty CanPasteClipboardContentProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(CanPasteClipboardContent), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.TextBox), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.TextBox.TextBox()
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.TextBox()
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.Text.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.Text.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.SelectedText.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.SelectedText.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.SelectionLength.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.SelectionLength.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.SelectionStart.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.SelectionStart.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.MaxLength.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.MaxLength.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.IsReadOnly.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.IsReadOnly.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.AcceptsReturn.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.AcceptsReturn.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.TextAlignment.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.TextAlignment.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.TextWrapping.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.TextWrapping.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.IsSpellCheckEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.IsSpellCheckEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.IsTextPredictionEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.IsTextPredictionEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.InputScope.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.InputScope.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.TextChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.TextChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.SelectionChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.SelectionChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.ContextMenuOpening.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.ContextMenuOpening.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Select( int start,  int length)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "void TextBox.Select(int start, int length)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SelectAll()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "void TextBox.SelectAll()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Rect GetRectFromCharacterIndex( int charIndex,  bool trailingEdge)
		{
			throw new global::System.NotImplementedException("The member Rect TextBox.GetRectFromCharacterIndex(int charIndex, bool trailingEdge) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.Header.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.Header.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.HeaderTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.HeaderTemplate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.PlaceholderText.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.PlaceholderText.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.SelectionHighlightColor.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.SelectionHighlightColor.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.PreventKeyboardDisplayOnProgrammaticFocus.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.PreventKeyboardDisplayOnProgrammaticFocus.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.IsColorFontEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.IsColorFontEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.Paste.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.Paste.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.TextCompositionStarted.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.TextCompositionStarted.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.TextCompositionChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.TextCompositionChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.TextCompositionEnded.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.TextCompositionEnded.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.TextReadingOrder.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.TextReadingOrder.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.DesiredCandidateWindowAlignment.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.DesiredCandidateWindowAlignment.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.CandidateWindowBoundsChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.CandidateWindowBoundsChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.TextChanging.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.TextChanging.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<string>> GetLinguisticAlternativesAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<string>> TextBox.GetLinguisticAlternativesAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.SelectionHighlightColorWhenNotFocused.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.SelectionHighlightColorWhenNotFocused.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.HorizontalTextAlignment.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.HorizontalTextAlignment.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.CharacterCasing.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.CharacterCasing.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.PlaceholderForeground.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.PlaceholderForeground.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.CopyingToClipboard.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.CopyingToClipboard.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.CuttingToClipboard.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.CuttingToClipboard.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.BeforeTextChanging.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.BeforeTextChanging.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.HandwritingView.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.HandwritingView.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.IsHandwritingViewEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.IsHandwritingViewEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.CanPasteClipboardContent.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.CanUndo.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.CanRedo.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.SelectionFlyout.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.SelectionFlyout.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.ProofingMenuFlyout.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.Description.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.Description.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.SelectionChanging.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.SelectionChanging.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Undo()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "void TextBox.Undo()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Redo()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "void TextBox.Redo()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void PasteFromClipboard()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "void TextBox.PasteFromClipboard()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void CopySelectionToClipboard()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "void TextBox.CopySelectionToClipboard()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void CutSelectionToClipboard()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "void TextBox.CutSelectionToClipboard()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ClearUndoRedoHistory()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "void TextBox.ClearUndoRedoHistory()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.CanPasteClipboardContentProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.CanUndoProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.CanRedoProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.SelectionFlyoutProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.ProofingMenuFlyoutProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.DescriptionProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.HandwritingViewProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.IsHandwritingViewEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.HorizontalTextAlignmentProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.CharacterCasingProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.PlaceholderForegroundProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.SelectionHighlightColorWhenNotFocusedProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.DesiredCandidateWindowAlignmentProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.TextReadingOrderProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.HeaderProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.HeaderTemplateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.PlaceholderTextProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.SelectionHighlightColorProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.PreventKeyboardDisplayOnProgrammaticFocusProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.IsColorFontEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.TextProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.MaxLengthProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.IsReadOnlyProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.AcceptsReturnProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.TextAlignmentProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.TextWrappingProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.IsSpellCheckEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.IsTextPredictionEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TextBox.InputScopeProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.UI.Xaml.Controls.ContextMenuOpeningEventHandler ContextMenuOpening
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "event ContextMenuOpeningEventHandler TextBox.ContextMenuOpening");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "event ContextMenuOpeningEventHandler TextBox.ContextMenuOpening");
			}
		}
		#endif
		// Skipping already declared event Windows.UI.Xaml.Controls.TextBox.SelectionChanged
		// Skipping already declared event Windows.UI.Xaml.Controls.TextBox.TextChanged
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.UI.Xaml.Controls.TextControlPasteEventHandler Paste
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "event TextControlPasteEventHandler TextBox.Paste");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "event TextControlPasteEventHandler TextBox.Paste");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.TextBox, global::Windows.UI.Xaml.Controls.CandidateWindowBoundsChangedEventArgs> CandidateWindowBoundsChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "event TypedEventHandler<TextBox, CandidateWindowBoundsChangedEventArgs> TextBox.CandidateWindowBoundsChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "event TypedEventHandler<TextBox, CandidateWindowBoundsChangedEventArgs> TextBox.CandidateWindowBoundsChanged");
			}
		}
		#endif
		// Skipping already declared event Windows.UI.Xaml.Controls.TextBox.TextChanging
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.TextBox, global::Windows.UI.Xaml.Controls.TextCompositionChangedEventArgs> TextCompositionChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "event TypedEventHandler<TextBox, TextCompositionChangedEventArgs> TextBox.TextCompositionChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "event TypedEventHandler<TextBox, TextCompositionChangedEventArgs> TextBox.TextCompositionChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.TextBox, global::Windows.UI.Xaml.Controls.TextCompositionEndedEventArgs> TextCompositionEnded
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "event TypedEventHandler<TextBox, TextCompositionEndedEventArgs> TextBox.TextCompositionEnded");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "event TypedEventHandler<TextBox, TextCompositionEndedEventArgs> TextBox.TextCompositionEnded");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.TextBox, global::Windows.UI.Xaml.Controls.TextCompositionStartedEventArgs> TextCompositionStarted
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "event TypedEventHandler<TextBox, TextCompositionStartedEventArgs> TextBox.TextCompositionStarted");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "event TypedEventHandler<TextBox, TextCompositionStartedEventArgs> TextBox.TextCompositionStarted");
			}
		}
		#endif
		// Skipping already declared event Windows.UI.Xaml.Controls.TextBox.BeforeTextChanging
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.TextBox, global::Windows.UI.Xaml.Controls.TextControlCopyingToClipboardEventArgs> CopyingToClipboard
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "event TypedEventHandler<TextBox, TextControlCopyingToClipboardEventArgs> TextBox.CopyingToClipboard");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "event TypedEventHandler<TextBox, TextControlCopyingToClipboardEventArgs> TextBox.CopyingToClipboard");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.TextBox, global::Windows.UI.Xaml.Controls.TextControlCuttingToClipboardEventArgs> CuttingToClipboard
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "event TypedEventHandler<TextBox, TextControlCuttingToClipboardEventArgs> TextBox.CuttingToClipboard");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "event TypedEventHandler<TextBox, TextControlCuttingToClipboardEventArgs> TextBox.CuttingToClipboard");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.TextBox, global::Windows.UI.Xaml.Controls.TextBoxSelectionChangingEventArgs> SelectionChanging
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "event TypedEventHandler<TextBox, TextBoxSelectionChangingEventArgs> TextBox.SelectionChanging");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TextBox", "event TypedEventHandler<TextBox, TextBoxSelectionChangingEventArgs> TextBox.SelectionChanging");
			}
		}
		#endif
	}
}
