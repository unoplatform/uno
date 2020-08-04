#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WwwFormUrlDecoder : global::System.Collections.Generic.IReadOnlyList<global::Windows.Foundation.IWwwFormUrlDecoderEntry>,global::System.Collections.Generic.IEnumerable<global::Windows.Foundation.IWwwFormUrlDecoderEntry>
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Size
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint WwwFormUrlDecoder.Size is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public WwwFormUrlDecoder( string query) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.WwwFormUrlDecoder", "WwwFormUrlDecoder.WwwFormUrlDecoder(string query)");
		}
		#endif
		// Forced skipping of method Windows.Foundation.WwwFormUrlDecoder.WwwFormUrlDecoder(string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string GetFirstValueByName( string name)
		{
			throw new global::System.NotImplementedException("The member string WwwFormUrlDecoder.GetFirstValueByName(string name) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Foundation.WwwFormUrlDecoder.First()
		// Forced skipping of method Windows.Foundation.WwwFormUrlDecoder.GetAt(uint)
		// Forced skipping of method Windows.Foundation.WwwFormUrlDecoder.Size.get
		// Forced skipping of method Windows.Foundation.WwwFormUrlDecoder.IndexOf(Windows.Foundation.IWwwFormUrlDecoderEntry, out uint)
		// Forced skipping of method Windows.Foundation.WwwFormUrlDecoder.GetMany(uint, Windows.Foundation.IWwwFormUrlDecoderEntry[])
		// Processing: System.Collections.Generic.IReadOnlyList<Windows.Foundation.IWwwFormUrlDecoderEntry>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::Windows.Foundation.IWwwFormUrlDecoderEntry this[int index]
		{
			get
			{
				throw new global::System.NotSupportedException();
			}
			set
			{
				throw new global::System.NotSupportedException();
			}
		}
		#endif
		// Processing: System.Collections.Generic.IEnumerable<Windows.Foundation.IWwwFormUrlDecoderEntry>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IEnumerable<Windows.Foundation.IWwwFormUrlDecoderEntry>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::System.Collections.Generic.IEnumerator<global::Windows.Foundation.IWwwFormUrlDecoderEntry> GetEnumerator()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		// Processing: System.Collections.IEnumerable
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.IEnumerable
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		 global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		// Processing: System.Collections.Generic.IReadOnlyCollection<Windows.Foundation.IWwwFormUrlDecoderEntry>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public int Count
		{
			get
			{
				throw new global::System.NotSupportedException();
			}
			set
			{
				throw new global::System.NotSupportedException();
			}
		}
		#endif
	}
}
