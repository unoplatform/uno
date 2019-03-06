#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CanExecuteRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool CanExecute
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CanExecuteRequestedEventArgs.CanExecute is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.CanExecuteRequestedEventArgs", "bool CanExecuteRequestedEventArgs.CanExecute");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  object Parameter
		{
			get
			{
				throw new global::System.NotImplementedException("The member object CanExecuteRequestedEventArgs.Parameter is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Input.CanExecuteRequestedEventArgs.Parameter.get
		// Forced skipping of method Windows.UI.Xaml.Input.CanExecuteRequestedEventArgs.CanExecute.get
		// Forced skipping of method Windows.UI.Xaml.Input.CanExecuteRequestedEventArgs.CanExecute.set
	}
}
