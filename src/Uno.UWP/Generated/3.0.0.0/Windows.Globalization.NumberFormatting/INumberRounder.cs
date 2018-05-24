#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Globalization.NumberFormatting
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface INumberRounder 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		int RoundInt32( int value);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		uint RoundUInt32( uint value);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		long RoundInt64( long value);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ulong RoundUInt64( ulong value);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		float RoundSingle( float value);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		double RoundDouble( double value);
		#endif
	}
}
