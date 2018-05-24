#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Globalization.NumberFormatting
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface INumberFormatter2 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		string FormatInt( long value);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		string FormatUInt( ulong value);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		string FormatDouble( double value);
		#endif
	}
}
