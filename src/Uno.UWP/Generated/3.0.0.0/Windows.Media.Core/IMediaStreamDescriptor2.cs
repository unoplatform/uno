#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IMediaStreamDescriptor2 : global::Windows.Media.Core.IMediaStreamDescriptor
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Label
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Media.Core.IMediaStreamDescriptor2.Label.set
		// Forced skipping of method Windows.Media.Core.IMediaStreamDescriptor2.Label.get
	}
}
