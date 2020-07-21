#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MseSourceBufferList 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Core.MseSourceBuffer> Buffers
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<MseSourceBuffer> MseSourceBufferList.Buffers is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Core.MseSourceBufferList.SourceBufferAdded.add
		// Forced skipping of method Windows.Media.Core.MseSourceBufferList.SourceBufferAdded.remove
		// Forced skipping of method Windows.Media.Core.MseSourceBufferList.SourceBufferRemoved.add
		// Forced skipping of method Windows.Media.Core.MseSourceBufferList.SourceBufferRemoved.remove
		// Forced skipping of method Windows.Media.Core.MseSourceBufferList.Buffers.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Core.MseSourceBufferList, object> SourceBufferAdded
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBufferList", "event TypedEventHandler<MseSourceBufferList, object> MseSourceBufferList.SourceBufferAdded");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBufferList", "event TypedEventHandler<MseSourceBufferList, object> MseSourceBufferList.SourceBufferAdded");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Core.MseSourceBufferList, object> SourceBufferRemoved
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBufferList", "event TypedEventHandler<MseSourceBufferList, object> MseSourceBufferList.SourceBufferRemoved");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBufferList", "event TypedEventHandler<MseSourceBufferList, object> MseSourceBufferList.SourceBufferRemoved");
			}
		}
		#endif
	}
}
