#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Data.Xml.Dom
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IXmlNode : global::Windows.Data.Xml.Dom.IXmlNodeSelector,global::Windows.Data.Xml.Dom.IXmlNodeSerializer
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		global::Windows.Data.Xml.Dom.XmlNamedNodeMap Attributes
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		global::Windows.Data.Xml.Dom.XmlNodeList ChildNodes
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		global::Windows.Data.Xml.Dom.IXmlNode FirstChild
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		global::Windows.Data.Xml.Dom.IXmlNode LastChild
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		object LocalName
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		object NamespaceUri
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		global::Windows.Data.Xml.Dom.IXmlNode NextSibling
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		string NodeName
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		global::Windows.Data.Xml.Dom.NodeType NodeType
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		object NodeValue
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		global::Windows.Data.Xml.Dom.XmlDocument OwnerDocument
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		global::Windows.Data.Xml.Dom.IXmlNode ParentNode
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		object Prefix
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		global::Windows.Data.Xml.Dom.IXmlNode PreviousSibling
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Data.Xml.Dom.IXmlNode.NodeValue.get
		// Forced skipping of method Windows.Data.Xml.Dom.IXmlNode.NodeValue.set
		// Forced skipping of method Windows.Data.Xml.Dom.IXmlNode.NodeType.get
		// Forced skipping of method Windows.Data.Xml.Dom.IXmlNode.NodeName.get
		// Forced skipping of method Windows.Data.Xml.Dom.IXmlNode.ParentNode.get
		// Forced skipping of method Windows.Data.Xml.Dom.IXmlNode.ChildNodes.get
		// Forced skipping of method Windows.Data.Xml.Dom.IXmlNode.FirstChild.get
		// Forced skipping of method Windows.Data.Xml.Dom.IXmlNode.LastChild.get
		// Forced skipping of method Windows.Data.Xml.Dom.IXmlNode.PreviousSibling.get
		// Forced skipping of method Windows.Data.Xml.Dom.IXmlNode.NextSibling.get
		// Forced skipping of method Windows.Data.Xml.Dom.IXmlNode.Attributes.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		bool HasChildNodes();
		#endif
		// Forced skipping of method Windows.Data.Xml.Dom.IXmlNode.OwnerDocument.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		global::Windows.Data.Xml.Dom.IXmlNode InsertBefore( global::Windows.Data.Xml.Dom.IXmlNode newChild,  global::Windows.Data.Xml.Dom.IXmlNode referenceChild);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		global::Windows.Data.Xml.Dom.IXmlNode ReplaceChild( global::Windows.Data.Xml.Dom.IXmlNode newChild,  global::Windows.Data.Xml.Dom.IXmlNode referenceChild);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		global::Windows.Data.Xml.Dom.IXmlNode RemoveChild( global::Windows.Data.Xml.Dom.IXmlNode childNode);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		global::Windows.Data.Xml.Dom.IXmlNode AppendChild( global::Windows.Data.Xml.Dom.IXmlNode newChild);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		global::Windows.Data.Xml.Dom.IXmlNode CloneNode( bool deep);
		#endif
		// Forced skipping of method Windows.Data.Xml.Dom.IXmlNode.NamespaceUri.get
		// Forced skipping of method Windows.Data.Xml.Dom.IXmlNode.LocalName.get
		// Forced skipping of method Windows.Data.Xml.Dom.IXmlNode.Prefix.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		void Normalize();
		#endif
		// Forced skipping of method Windows.Data.Xml.Dom.IXmlNode.Prefix.set
	}
}
