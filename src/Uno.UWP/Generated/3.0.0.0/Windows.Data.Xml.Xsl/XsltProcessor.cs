#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Data.Xml.Xsl
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class XsltProcessor 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public XsltProcessor( global::Windows.Data.Xml.Dom.XmlDocument document) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Xml.Xsl.XsltProcessor", "XsltProcessor.XsltProcessor(XmlDocument document)");
		}
		#endif
		// Forced skipping of method Windows.Data.Xml.Xsl.XsltProcessor.XsltProcessor(Windows.Data.Xml.Dom.XmlDocument)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string TransformToString( global::Windows.Data.Xml.Dom.IXmlNode inputNode)
		{
			throw new global::System.NotImplementedException("The member string XsltProcessor.TransformToString(IXmlNode inputNode) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20XsltProcessor.TransformToString%28IXmlNode%20inputNode%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Data.Xml.Dom.XmlDocument TransformToDocument( global::Windows.Data.Xml.Dom.IXmlNode inputNode)
		{
			throw new global::System.NotImplementedException("The member XmlDocument XsltProcessor.TransformToDocument(IXmlNode inputNode) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=XmlDocument%20XsltProcessor.TransformToDocument%28IXmlNode%20inputNode%29");
		}
		#endif
	}
}
