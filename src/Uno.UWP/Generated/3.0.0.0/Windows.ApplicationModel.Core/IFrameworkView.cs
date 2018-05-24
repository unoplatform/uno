#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IFrameworkView 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void Initialize( global::Windows.ApplicationModel.Core.CoreApplicationView applicationView);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void SetWindow( global::Windows.UI.Core.CoreWindow window);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void Load( string entryPoint);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void Run();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void Uninitialize();
		#endif
	}
}
