#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.PrintTicket
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintTicketValue 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.PrintTicket.PrintTicketValueType Type
		{
			get
			{
				throw new global::System.NotImplementedException("The member PrintTicketValueType PrintTicketValue.Type is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.PrintTicket.PrintTicketValue.Type.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int GetValueAsInteger()
		{
			throw new global::System.NotImplementedException("The member int PrintTicketValue.GetValueAsInteger() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string GetValueAsString()
		{
			throw new global::System.NotImplementedException("The member string PrintTicketValue.GetValueAsString() is not implemented in Uno.");
		}
		#endif
	}
}
