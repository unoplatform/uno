#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Threading
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	public delegate void TimerElapsedHandler(global::Windows.System.Threading.ThreadPoolTimer @timer);
	#endif
}
