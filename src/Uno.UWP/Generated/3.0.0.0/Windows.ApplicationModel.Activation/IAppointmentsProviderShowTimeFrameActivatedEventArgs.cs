#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Activation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IAppointmentsProviderShowTimeFrameActivatedEventArgs : global::Windows.ApplicationModel.Activation.IAppointmentsProviderActivatedEventArgs,global::Windows.ApplicationModel.Activation.IActivatedEventArgs
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.TimeSpan Duration
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.DateTimeOffset TimeToShow
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Activation.IAppointmentsProviderShowTimeFrameActivatedEventArgs.TimeToShow.get
		// Forced skipping of method Windows.ApplicationModel.Activation.IAppointmentsProviderShowTimeFrameActivatedEventArgs.Duration.get
	}
}
