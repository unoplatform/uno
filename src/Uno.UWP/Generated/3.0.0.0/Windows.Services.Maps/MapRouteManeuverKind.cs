#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Maps
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MapRouteManeuverKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Start,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Stopover,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StopoverResume,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		End,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GoStraight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UTurnLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UTurnRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TurnKeepLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TurnKeepRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TurnLightLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TurnLightRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TurnLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TurnRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TurnHardLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TurnHardRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FreewayEnterLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FreewayEnterRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FreewayLeaveLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FreewayLeaveRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FreewayContinueLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FreewayContinueRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TrafficCircleLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TrafficCircleRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TakeFerry,
		#endif
	}
	#endif
}
