#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class XamlUICommand : global::Windows.UI.Xaml.DependencyObject,global::System.Windows.Input.ICommand
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Label
		{
			get
			{
				return (string)this.GetValue(LabelProperty);
			}
			set
			{
				this.SetValue(LabelProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.IconSource IconSource
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.IconSource)this.GetValue(IconSourceProperty);
			}
			set
			{
				this.SetValue(IconSourceProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Description
		{
			get
			{
				return (string)this.GetValue(DescriptionProperty);
			}
			set
			{
				this.SetValue(DescriptionProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Windows.Input.ICommand Command
		{
			get
			{
				return (global::System.Windows.Input.ICommand)this.GetValue(CommandProperty);
			}
			set
			{
				this.SetValue(CommandProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string AccessKey
		{
			get
			{
				return (string)this.GetValue(AccessKeyProperty);
			}
			set
			{
				this.SetValue(AccessKeyProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Input.KeyboardAccelerator> KeyboardAccelerators
		{
			get
			{
				return (global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Input.KeyboardAccelerator>)this.GetValue(KeyboardAcceleratorsProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AccessKeyProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"AccessKey", typeof(string), 
			typeof(global::Windows.UI.Xaml.Input.XamlUICommand), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CommandProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Command", typeof(global::System.Windows.Input.ICommand), 
			typeof(global::Windows.UI.Xaml.Input.XamlUICommand), 
			new FrameworkPropertyMetadata(default(global::System.Windows.Input.ICommand)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DescriptionProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Description", typeof(string), 
			typeof(global::Windows.UI.Xaml.Input.XamlUICommand), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IconSourceProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IconSource", typeof(global::Windows.UI.Xaml.Controls.IconSource), 
			typeof(global::Windows.UI.Xaml.Input.XamlUICommand), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.IconSource)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty KeyboardAcceleratorsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"KeyboardAccelerators", typeof(global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Input.KeyboardAccelerator>), 
			typeof(global::Windows.UI.Xaml.Input.XamlUICommand), 
			new FrameworkPropertyMetadata(default(global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Input.KeyboardAccelerator>)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty LabelProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Label", typeof(string), 
			typeof(global::Windows.UI.Xaml.Input.XamlUICommand), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Input.XamlUICommand.XamlUICommand()
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.XamlUICommand()
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.Label.get
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.Label.set
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.IconSource.get
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.IconSource.set
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.KeyboardAccelerators.get
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.AccessKey.get
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.AccessKey.set
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.Description.get
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.Description.set
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.Command.get
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.Command.set
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.ExecuteRequested.add
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.ExecuteRequested.remove
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.CanExecuteRequested.add
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.CanExecuteRequested.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void NotifyCanExecuteChanged()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.XamlUICommand", "void XamlUICommand.NotifyCanExecuteChanged()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.CanExecuteChanged.add
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.CanExecuteChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.CanExecute(object)
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.Execute(object)
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.LabelProperty.get
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.IconSourceProperty.get
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.KeyboardAcceleratorsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.AccessKeyProperty.get
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.DescriptionProperty.get
		// Forced skipping of method Windows.UI.Xaml.Input.XamlUICommand.CommandProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Input.XamlUICommand, global::Windows.UI.Xaml.Input.CanExecuteRequestedEventArgs> CanExecuteRequested
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.XamlUICommand", "event TypedEventHandler<XamlUICommand, CanExecuteRequestedEventArgs> XamlUICommand.CanExecuteRequested");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.XamlUICommand", "event TypedEventHandler<XamlUICommand, CanExecuteRequestedEventArgs> XamlUICommand.CanExecuteRequested");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Input.XamlUICommand, global::Windows.UI.Xaml.Input.ExecuteRequestedEventArgs> ExecuteRequested
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.XamlUICommand", "event TypedEventHandler<XamlUICommand, ExecuteRequestedEventArgs> XamlUICommand.ExecuteRequested");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.XamlUICommand", "event TypedEventHandler<XamlUICommand, ExecuteRequestedEventArgs> XamlUICommand.ExecuteRequested");
			}
		}
		#endif
		// Processing: System.Windows.Input.ICommand
	}
}
