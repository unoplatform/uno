#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Data.Xml.Dom
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IXmlNodeSelector 
	{
		// Skipping already declared method Windows.Data.Xml.Dom.IXmlNodeSelector.SelectSingleNode(string)
		// Skipping already declared method Windows.Data.Xml.Dom.IXmlNodeSelector.SelectNodes(string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Data.Xml.Dom.IXmlNode SelectSingleNodeNS( string xpath,  object namespaces);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Data.Xml.Dom.XmlNodeList SelectNodesNS( string xpath,  object namespaces);
		#endif
	}
}
