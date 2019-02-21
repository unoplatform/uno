#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PartialMediaFailureDetectedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Playback.FailedMediaStreamKind StreamKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member FailedMediaStreamKind PartialMediaFailureDetectedEventArgs.StreamKind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Exception ExtendedError
		{
			get
			{
				throw new global::System.NotImplementedException("The member Exception PartialMediaFailureDetectedEventArgs.ExtendedError is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public PartialMediaFailureDetectedEventArgs() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.PartialMediaFailureDetectedEventArgs", "PartialMediaFailureDetectedEventArgs.PartialMediaFailureDetectedEventArgs()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.PartialMediaFailureDetectedEventArgs.PartialMediaFailureDetectedEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Media.PartialMediaFailureDetectedEventArgs.StreamKind.get
		// Forced skipping of method Windows.UI.Xaml.Media.PartialMediaFailureDetectedEventArgs.ExtendedError.get
	}
}
