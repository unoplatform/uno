#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Globalization.NumberFormatting
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface INumberParser 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		long? ParseInt( string text);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ulong? ParseUInt( string text);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		double? ParseDouble( string text);
		#endif
	}
}
