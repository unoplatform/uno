#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CornerRadiusHelper 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.CornerRadius FromRadii( double topLeft,  double topRight,  double bottomRight,  double bottomLeft)
		{
			throw new global::System.NotImplementedException("The member CornerRadius CornerRadiusHelper.FromRadii(double topLeft, double topRight, double bottomRight, double bottomLeft) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.CornerRadius FromUniformRadius( double uniformRadius)
		{
			throw new global::System.NotImplementedException("The member CornerRadius CornerRadiusHelper.FromUniformRadius(double uniformRadius) is not implemented in Uno.");
		}
		#endif
	}
}
