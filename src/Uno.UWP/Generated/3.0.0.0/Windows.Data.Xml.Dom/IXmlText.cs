#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Data.Xml.Dom
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IXmlText : global::Windows.Data.Xml.Dom.IXmlCharacterData,global::Windows.Data.Xml.Dom.IXmlNode,global::Windows.Data.Xml.Dom.IXmlNodeSelector,global::Windows.Data.Xml.Dom.IXmlNodeSerializer
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		global::Windows.Data.Xml.Dom.IXmlText SplitText( uint offset);
		#endif
	}
}
