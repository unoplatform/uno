#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CodecQuery 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public CodecQuery() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.CodecQuery", "CodecQuery.CodecQuery()");
		}
		#endif
		// Forced skipping of method Windows.Media.Core.CodecQuery.CodecQuery()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Core.CodecInfo>> FindAllAsync( global::Windows.Media.Core.CodecKind kind,  global::Windows.Media.Core.CodecCategory category,  string subType)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<CodecInfo>> CodecQuery.FindAllAsync(CodecKind kind, CodecCategory category, string subType) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CIReadOnlyList%3CCodecInfo%3E%3E%20CodecQuery.FindAllAsync%28CodecKind%20kind%2C%20CodecCategory%20category%2C%20string%20subType%29");
		}
		#endif
	}
}
