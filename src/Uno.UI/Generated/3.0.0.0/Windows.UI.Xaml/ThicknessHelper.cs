#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ThicknessHelper 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.Thickness FromLengths( double left,  double top,  double right,  double bottom)
		{
			throw new global::System.NotImplementedException("The member Thickness ThicknessHelper.FromLengths(double left, double top, double right, double bottom) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.Thickness FromUniformLength( double uniformLength)
		{
			throw new global::System.NotImplementedException("The member Thickness ThicknessHelper.FromUniformLength(double uniformLength) is not implemented in Uno.");
		}
		#endif
	}
}
