#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ProvisionFromXmlDocumentResults 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool AllElementsProvisioned
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ProvisionFromXmlDocumentResults.AllElementsProvisioned is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ProvisionResultsXml
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ProvisionFromXmlDocumentResults.ProvisionResultsXml is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.ProvisionFromXmlDocumentResults.AllElementsProvisioned.get
		// Forced skipping of method Windows.Networking.NetworkOperators.ProvisionFromXmlDocumentResults.ProvisionResultsXml.get
	}
}
