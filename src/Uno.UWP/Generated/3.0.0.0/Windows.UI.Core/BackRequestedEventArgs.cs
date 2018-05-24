#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class BackRequestedEventArgs 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool BackRequestedEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.BackRequestedEventArgs", "bool BackRequestedEventArgs.Handled");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Core.BackRequestedEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Core.BackRequestedEventArgs.Handled.set
	}
}
