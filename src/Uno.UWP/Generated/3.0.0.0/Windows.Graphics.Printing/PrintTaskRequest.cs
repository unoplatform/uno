#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintTaskRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset Deadline
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset PrintTaskRequest.Deadline is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DateTimeOffset%20PrintTaskRequest.Deadline");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.PrintTaskRequest.Deadline.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.PrintTask CreatePrintTask( string title,  global::Windows.Graphics.Printing.PrintTaskSourceRequestedHandler handler)
		{
			throw new global::System.NotImplementedException("The member PrintTask PrintTaskRequest.CreatePrintTask(string title, PrintTaskSourceRequestedHandler handler) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PrintTask%20PrintTaskRequest.CreatePrintTask%28string%20title%2C%20PrintTaskSourceRequestedHandler%20handler%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.PrintTaskRequestedDeferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member PrintTaskRequestedDeferral PrintTaskRequest.GetDeferral() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PrintTaskRequestedDeferral%20PrintTaskRequest.GetDeferral%28%29");
		}
		#endif
	}
}
