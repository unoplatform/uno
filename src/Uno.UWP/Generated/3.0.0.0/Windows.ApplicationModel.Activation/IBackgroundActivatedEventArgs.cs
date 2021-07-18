#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Activation
{
	#if false || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IBackgroundActivatedEventArgs 
	{
		#if false || __IOS__ || NET461 || __WASM__ || __MACOS__
		global::Windows.ApplicationModel.Background.IBackgroundTaskInstance TaskInstance
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Activation.IBackgroundActivatedEventArgs.TaskInstance.get
	}
}
