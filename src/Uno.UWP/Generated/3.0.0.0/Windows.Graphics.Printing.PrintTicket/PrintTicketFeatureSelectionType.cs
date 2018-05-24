#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.PrintTicket
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PrintTicketFeatureSelectionType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PickOne,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PickMany,
		#endif
	}
	#endif
}
