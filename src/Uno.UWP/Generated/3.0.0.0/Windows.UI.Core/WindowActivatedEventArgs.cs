#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class WindowActivatedEventArgs : global::Windows.UI.Core.ICoreWindowEventArgs
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool WindowActivatedEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.WindowActivatedEventArgs", "bool WindowActivatedEventArgs.Handled");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Core.CoreWindowActivationState WindowActivationState
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreWindowActivationState WindowActivatedEventArgs.WindowActivationState is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Core.WindowActivatedEventArgs.WindowActivationState.get
		// Forced skipping of method Windows.UI.Core.WindowActivatedEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Core.WindowActivatedEventArgs.Handled.set
		// Processing: Windows.UI.Core.ICoreWindowEventArgs
	}
}
