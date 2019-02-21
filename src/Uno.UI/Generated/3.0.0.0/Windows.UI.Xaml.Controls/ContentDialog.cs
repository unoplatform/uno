#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContentDialog : global::Windows.UI.Xaml.Controls.ContentControl
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.DataTemplate TitleTemplate
		{
			get
			{
				return (global::Windows.UI.Xaml.DataTemplate)this.GetValue(TitleTemplateProperty);
			}
			set
			{
				this.SetValue(TitleTemplateProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  object Title
		{
			get
			{
				return (object)this.GetValue(TitleProperty);
			}
			set
			{
				this.SetValue(TitleProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string SecondaryButtonText
		{
			get
			{
				return (string)this.GetValue(SecondaryButtonTextProperty);
			}
			set
			{
				this.SetValue(SecondaryButtonTextProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  object SecondaryButtonCommandParameter
		{
			get
			{
				return (object)this.GetValue(SecondaryButtonCommandParameterProperty);
			}
			set
			{
				this.SetValue(SecondaryButtonCommandParameterProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Windows.Input.ICommand SecondaryButtonCommand
		{
			get
			{
				return (global::System.Windows.Input.ICommand)this.GetValue(SecondaryButtonCommandProperty);
			}
			set
			{
				this.SetValue(SecondaryButtonCommandProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string PrimaryButtonText
		{
			get
			{
				return (string)this.GetValue(PrimaryButtonTextProperty);
			}
			set
			{
				this.SetValue(PrimaryButtonTextProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  object PrimaryButtonCommandParameter
		{
			get
			{
				return (object)this.GetValue(PrimaryButtonCommandParameterProperty);
			}
			set
			{
				this.SetValue(PrimaryButtonCommandParameterProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Windows.Input.ICommand PrimaryButtonCommand
		{
			get
			{
				return (global::System.Windows.Input.ICommand)this.GetValue(PrimaryButtonCommandProperty);
			}
			set
			{
				this.SetValue(PrimaryButtonCommandProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsSecondaryButtonEnabled
		{
			get
			{
				return (bool)this.GetValue(IsSecondaryButtonEnabledProperty);
			}
			set
			{
				this.SetValue(IsSecondaryButtonEnabledProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsPrimaryButtonEnabled
		{
			get
			{
				return (bool)this.GetValue(IsPrimaryButtonEnabledProperty);
			}
			set
			{
				this.SetValue(IsPrimaryButtonEnabledProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool FullSizeDesired
		{
			get
			{
				return (bool)this.GetValue(FullSizeDesiredProperty);
			}
			set
			{
				this.SetValue(FullSizeDesiredProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Style SecondaryButtonStyle
		{
			get
			{
				return (global::Windows.UI.Xaml.Style)this.GetValue(SecondaryButtonStyleProperty);
			}
			set
			{
				this.SetValue(SecondaryButtonStyleProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Style PrimaryButtonStyle
		{
			get
			{
				return (global::Windows.UI.Xaml.Style)this.GetValue(PrimaryButtonStyleProperty);
			}
			set
			{
				this.SetValue(PrimaryButtonStyleProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.ContentDialogButton DefaultButton
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.ContentDialogButton)this.GetValue(DefaultButtonProperty);
			}
			set
			{
				this.SetValue(DefaultButtonProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string CloseButtonText
		{
			get
			{
				return (string)this.GetValue(CloseButtonTextProperty);
			}
			set
			{
				this.SetValue(CloseButtonTextProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Style CloseButtonStyle
		{
			get
			{
				return (global::Windows.UI.Xaml.Style)this.GetValue(CloseButtonStyleProperty);
			}
			set
			{
				this.SetValue(CloseButtonStyleProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  object CloseButtonCommandParameter
		{
			get
			{
				return (object)this.GetValue(CloseButtonCommandParameterProperty);
			}
			set
			{
				this.SetValue(CloseButtonCommandParameterProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Windows.Input.ICommand CloseButtonCommand
		{
			get
			{
				return (global::System.Windows.Input.ICommand)this.GetValue(CloseButtonCommandProperty);
			}
			set
			{
				this.SetValue(CloseButtonCommandProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty FullSizeDesiredProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"FullSizeDesired", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ContentDialog), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsPrimaryButtonEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsPrimaryButtonEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ContentDialog), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsSecondaryButtonEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsSecondaryButtonEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ContentDialog), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PrimaryButtonCommandParameterProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PrimaryButtonCommandParameter", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.ContentDialog), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PrimaryButtonCommandProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PrimaryButtonCommand", typeof(global::System.Windows.Input.ICommand), 
			typeof(global::Windows.UI.Xaml.Controls.ContentDialog), 
			new FrameworkPropertyMetadata(default(global::System.Windows.Input.ICommand)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PrimaryButtonTextProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PrimaryButtonText", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.ContentDialog), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SecondaryButtonCommandParameterProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SecondaryButtonCommandParameter", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.ContentDialog), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SecondaryButtonCommandProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SecondaryButtonCommand", typeof(global::System.Windows.Input.ICommand), 
			typeof(global::Windows.UI.Xaml.Controls.ContentDialog), 
			new FrameworkPropertyMetadata(default(global::System.Windows.Input.ICommand)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SecondaryButtonTextProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SecondaryButtonText", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.ContentDialog), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TitleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Title", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.ContentDialog), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TitleTemplateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TitleTemplate", typeof(global::Windows.UI.Xaml.DataTemplate), 
			typeof(global::Windows.UI.Xaml.Controls.ContentDialog), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.DataTemplate)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CloseButtonCommandParameterProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CloseButtonCommandParameter", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.ContentDialog), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CloseButtonCommandProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CloseButtonCommand", typeof(global::System.Windows.Input.ICommand), 
			typeof(global::Windows.UI.Xaml.Controls.ContentDialog), 
			new FrameworkPropertyMetadata(default(global::System.Windows.Input.ICommand)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CloseButtonStyleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CloseButtonStyle", typeof(global::Windows.UI.Xaml.Style), 
			typeof(global::Windows.UI.Xaml.Controls.ContentDialog), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Style)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CloseButtonTextProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CloseButtonText", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.ContentDialog), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DefaultButtonProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"DefaultButton", typeof(global::Windows.UI.Xaml.Controls.ContentDialogButton), 
			typeof(global::Windows.UI.Xaml.Controls.ContentDialog), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.ContentDialogButton)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PrimaryButtonStyleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PrimaryButtonStyle", typeof(global::Windows.UI.Xaml.Style), 
			typeof(global::Windows.UI.Xaml.Controls.ContentDialog), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Style)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SecondaryButtonStyleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SecondaryButtonStyle", typeof(global::Windows.UI.Xaml.Style), 
			typeof(global::Windows.UI.Xaml.Controls.ContentDialog), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Style)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public ContentDialog() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ContentDialog", "ContentDialog.ContentDialog()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.ContentDialog()
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.Title.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.Title.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.TitleTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.TitleTemplate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.FullSizeDesired.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.FullSizeDesired.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.PrimaryButtonText.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.PrimaryButtonText.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.SecondaryButtonText.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.SecondaryButtonText.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.PrimaryButtonCommand.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.PrimaryButtonCommand.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.SecondaryButtonCommand.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.SecondaryButtonCommand.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.PrimaryButtonCommandParameter.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.PrimaryButtonCommandParameter.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.SecondaryButtonCommandParameter.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.SecondaryButtonCommandParameter.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.IsPrimaryButtonEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.IsPrimaryButtonEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.IsSecondaryButtonEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.IsSecondaryButtonEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.Closing.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.Closing.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.Closed.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.Closed.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.Opened.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.Opened.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.PrimaryButtonClick.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.PrimaryButtonClick.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.SecondaryButtonClick.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.SecondaryButtonClick.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Hide()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ContentDialog", "void ContentDialog.Hide()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.UI.Xaml.Controls.ContentDialogResult> ShowAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ContentDialogResult> ContentDialog.ShowAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.CloseButtonText.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.CloseButtonText.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.CloseButtonCommand.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.CloseButtonCommand.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.CloseButtonCommandParameter.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.CloseButtonCommandParameter.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.PrimaryButtonStyle.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.PrimaryButtonStyle.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.SecondaryButtonStyle.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.SecondaryButtonStyle.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.CloseButtonStyle.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.CloseButtonStyle.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.DefaultButton.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.DefaultButton.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.CloseButtonClick.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.CloseButtonClick.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.UI.Xaml.Controls.ContentDialogResult> ShowAsync( global::Windows.UI.Xaml.Controls.ContentDialogPlacement placement)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ContentDialogResult> ContentDialog.ShowAsync(ContentDialogPlacement placement) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.CloseButtonTextProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.CloseButtonCommandProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.CloseButtonCommandParameterProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.PrimaryButtonStyleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.SecondaryButtonStyleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.CloseButtonStyleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.DefaultButtonProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.TitleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.TitleTemplateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.FullSizeDesiredProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.PrimaryButtonTextProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.SecondaryButtonTextProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.PrimaryButtonCommandProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.SecondaryButtonCommandProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.PrimaryButtonCommandParameterProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.SecondaryButtonCommandParameterProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.IsPrimaryButtonEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialog.IsSecondaryButtonEnabledProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.ContentDialog, global::Windows.UI.Xaml.Controls.ContentDialogClosedEventArgs> Closed
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ContentDialog", "event TypedEventHandler<ContentDialog, ContentDialogClosedEventArgs> ContentDialog.Closed");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ContentDialog", "event TypedEventHandler<ContentDialog, ContentDialogClosedEventArgs> ContentDialog.Closed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.ContentDialog, global::Windows.UI.Xaml.Controls.ContentDialogClosingEventArgs> Closing
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ContentDialog", "event TypedEventHandler<ContentDialog, ContentDialogClosingEventArgs> ContentDialog.Closing");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ContentDialog", "event TypedEventHandler<ContentDialog, ContentDialogClosingEventArgs> ContentDialog.Closing");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.ContentDialog, global::Windows.UI.Xaml.Controls.ContentDialogOpenedEventArgs> Opened
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ContentDialog", "event TypedEventHandler<ContentDialog, ContentDialogOpenedEventArgs> ContentDialog.Opened");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ContentDialog", "event TypedEventHandler<ContentDialog, ContentDialogOpenedEventArgs> ContentDialog.Opened");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.ContentDialog, global::Windows.UI.Xaml.Controls.ContentDialogButtonClickEventArgs> PrimaryButtonClick
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ContentDialog", "event TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> ContentDialog.PrimaryButtonClick");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ContentDialog", "event TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> ContentDialog.PrimaryButtonClick");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.ContentDialog, global::Windows.UI.Xaml.Controls.ContentDialogButtonClickEventArgs> SecondaryButtonClick
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ContentDialog", "event TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> ContentDialog.SecondaryButtonClick");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ContentDialog", "event TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> ContentDialog.SecondaryButtonClick");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.ContentDialog, global::Windows.UI.Xaml.Controls.ContentDialogButtonClickEventArgs> CloseButtonClick
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ContentDialog", "event TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> ContentDialog.CloseButtonClick");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ContentDialog", "event TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> ContentDialog.CloseButtonClick");
			}
		}
		#endif
	}
}
