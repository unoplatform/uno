#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class WindowSizeChangedEventArgs : global::Windows.UI.Core.ICoreWindowEventArgs
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool WindowSizeChangedEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.WindowSizeChangedEventArgs", "bool WindowSizeChangedEventArgs.Handled");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Size Size
		{
			get
			{
				throw new global::System.NotImplementedException("The member Size WindowSizeChangedEventArgs.Size is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Core.WindowSizeChangedEventArgs.Size.get
		// Forced skipping of method Windows.UI.Core.WindowSizeChangedEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Core.WindowSizeChangedEventArgs.Handled.set
		// Processing: Windows.UI.Core.ICoreWindowEventArgs
	}
}
