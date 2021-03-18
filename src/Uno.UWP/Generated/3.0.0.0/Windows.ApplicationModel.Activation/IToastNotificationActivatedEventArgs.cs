#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Activation
{
	#if false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IToastNotificationActivatedEventArgs : global::Windows.ApplicationModel.Activation.IActivatedEventArgs
	{
		#if false
		string Argument
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.Collections.ValueSet UserInput
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Activation.IToastNotificationActivatedEventArgs.Argument.get
		// Forced skipping of method Windows.ApplicationModel.Activation.IToastNotificationActivatedEventArgs.UserInput.get
	}
}
