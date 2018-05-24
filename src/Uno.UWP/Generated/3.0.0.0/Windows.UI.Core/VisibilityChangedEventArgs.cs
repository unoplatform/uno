#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class VisibilityChangedEventArgs : global::Windows.UI.Core.ICoreWindowEventArgs
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool VisibilityChangedEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.VisibilityChangedEventArgs", "bool VisibilityChangedEventArgs.Handled");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool Visible
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool VisibilityChangedEventArgs.Visible is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Core.VisibilityChangedEventArgs.Visible.get
		// Forced skipping of method Windows.UI.Core.VisibilityChangedEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Core.VisibilityChangedEventArgs.Handled.set
		// Processing: Windows.UI.Core.ICoreWindowEventArgs
	}
}
