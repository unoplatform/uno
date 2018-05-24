#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ColorHelper 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static string ToDisplayName( global::Windows.UI.Color color)
		{
			throw new global::System.NotImplementedException("The member string ColorHelper.ToDisplayName(Color color) is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Color FromArgb( byte a,  byte r,  byte g,  byte b)
		{
			throw new global::System.NotImplementedException("The member Color ColorHelper.FromArgb(byte a, byte r, byte g, byte b) is not implemented in Uno.");
		}
		#endif
	}
}
