#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ISingleSelectMediaTrackList 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		int SelectedIndex
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Media.Core.ISingleSelectMediaTrackList.SelectedIndexChanged.add
		// Forced skipping of method Windows.Media.Core.ISingleSelectMediaTrackList.SelectedIndexChanged.remove
		// Forced skipping of method Windows.Media.Core.ISingleSelectMediaTrackList.SelectedIndex.set
		// Forced skipping of method Windows.Media.Core.ISingleSelectMediaTrackList.SelectedIndex.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Core.ISingleSelectMediaTrackList, object> SelectedIndexChanged;
		#endif
	}
}
