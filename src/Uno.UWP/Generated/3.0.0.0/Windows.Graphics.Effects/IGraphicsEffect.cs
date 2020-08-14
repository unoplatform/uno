#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Effects
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IGraphicsEffect : global::Windows.Graphics.Effects.IGraphicsEffectSource
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Name
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Graphics.Effects.IGraphicsEffect.Name.get
		// Forced skipping of method Windows.Graphics.Effects.IGraphicsEffect.Name.set
	}
}
