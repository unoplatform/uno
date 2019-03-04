#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintManager 
	{
		// Forced skipping of method Windows.Graphics.Printing.PrintManager.PrintTaskRequested.add
		// Forced skipping of method Windows.Graphics.Printing.PrintManager.PrintTaskRequested.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static bool IsSupported()
		{
			throw new global::System.NotImplementedException("The member bool PrintManager.IsSupported() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Graphics.Printing.PrintManager GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member PrintManager PrintManager.GetForCurrentView() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<bool> ShowPrintUIAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> PrintManager.ShowPrintUIAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Printing.PrintManager, global::Windows.Graphics.Printing.PrintTaskRequestedEventArgs> PrintTaskRequested
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintManager", "event TypedEventHandler<PrintManager, PrintTaskRequestedEventArgs> PrintManager.PrintTaskRequested");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintManager", "event TypedEventHandler<PrintManager, PrintTaskRequestedEventArgs> PrintManager.PrintTaskRequested");
			}
		}
		#endif
	}
}
