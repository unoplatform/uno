#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StandardUICommand : global::Windows.UI.Xaml.Input.XamlUICommand
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Input.StandardUICommandKind Kind
		{
			get
			{
				return (global::Windows.UI.Xaml.Input.StandardUICommandKind)this.GetValue(KindProperty);
			}
			set
			{
				this.SetValue(KindProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty KindProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Kind), typeof(global::Windows.UI.Xaml.Input.StandardUICommandKind), 
			typeof(global::Windows.UI.Xaml.Input.StandardUICommand), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Input.StandardUICommandKind)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public StandardUICommand() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.StandardUICommand", "StandardUICommand.StandardUICommand()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Input.StandardUICommand.StandardUICommand()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public StandardUICommand( global::Windows.UI.Xaml.Input.StandardUICommandKind kind) : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.StandardUICommand", "StandardUICommand.StandardUICommand(StandardUICommandKind kind)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Input.StandardUICommand.StandardUICommand(Windows.UI.Xaml.Input.StandardUICommandKind)
		// Forced skipping of method Windows.UI.Xaml.Input.StandardUICommand.Kind.get
		// Forced skipping of method Windows.UI.Xaml.Input.StandardUICommand.Kind.set
		// Forced skipping of method Windows.UI.Xaml.Input.StandardUICommand.KindProperty.get
	}
}
