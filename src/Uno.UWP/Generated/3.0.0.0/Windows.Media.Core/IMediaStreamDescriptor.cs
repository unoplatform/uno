#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IMediaStreamDescriptor 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsSelected
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Language
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Name
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Media.Core.IMediaStreamDescriptor.IsSelected.get
		// Forced skipping of method Windows.Media.Core.IMediaStreamDescriptor.Name.set
		// Forced skipping of method Windows.Media.Core.IMediaStreamDescriptor.Name.get
		// Forced skipping of method Windows.Media.Core.IMediaStreamDescriptor.Language.set
		// Forced skipping of method Windows.Media.Core.IMediaStreamDescriptor.Language.get
	}
}
