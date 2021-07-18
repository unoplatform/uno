#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Activation
{
	#if false || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BackgroundActivatedEventArgs : global::Windows.ApplicationModel.Activation.IBackgroundActivatedEventArgs
	{
		#if false || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.ApplicationModel.Background.IBackgroundTaskInstance TaskInstance
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBackgroundTaskInstance BackgroundActivatedEventArgs.TaskInstance is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Activation.BackgroundActivatedEventArgs.TaskInstance.get
		// Processing: Windows.ApplicationModel.Activation.IBackgroundActivatedEventArgs
	}
}
