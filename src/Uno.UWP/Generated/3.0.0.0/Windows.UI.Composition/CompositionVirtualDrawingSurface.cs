#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CompositionVirtualDrawingSurface : global::Windows.UI.Composition.CompositionDrawingSurface
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Trim( global::Windows.Graphics.RectInt32[] rects)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionVirtualDrawingSurface", "void CompositionVirtualDrawingSurface.Trim(RectInt32[] rects)");
		}
		#endif
	}
}
