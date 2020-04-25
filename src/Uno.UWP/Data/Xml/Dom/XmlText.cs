using SystemXmlText = System.Xml.XmlText;

namespace Windows.Data.Xml.Dom
{
	public partial class XmlText : IXmlText, IXmlCharacterData, IXmlNode, IXmlNodeSerializer, IXmlNodeSelector
	{
		private readonly XmlDocument _owner;
		private readonly SystemXmlText _backingText;

		internal XmlText(XmlDocument owner, SystemXmlText backingText)
		{
			_owner = owner;
			_backingText = backingText;
		}

		public string Data
		{
			get => _backingText.Data;
			set => _backingText.Data = value;
		}

		public uint Length => (uint)_backingText.Length;

		public object Prefix
		{
			get => _backingText.Prefix;
			set => _backingText.Prefix = value;
		}

		public object NodeValue
		{
			get => _backingText.Data;
			set => _backingText.Data = value?.ToString();
		}
		public IXmlNode FirstChild => (IXmlNode)_owner.Wrap(_backingText.FirstChild);

		public IXmlNode LastChild => (IXmlNode)_owner.Wrap(_backingText.LastChild);

		public object LocalName => _backingText.LocalName;

		public object NamespaceUri => _backingText.NamespaceURI;

		public IXmlNode NextSibling => (IXmlNode)_backingText.NextSibling;

		public string NodeName => _backingText.Name;

		public Dom.NodeType NodeType => Dom.NodeType.TextNode;

		public XmlNamedNodeMap Attributes
		{
			get
			{
				throw new global::System.NotImplementedException("The member XmlNamedNodeMap XmlText.Attributes is not implemented in Uno.");
			}
		}

		public XmlDocument OwnerDocument => _owner;

		public XmlNodeList ChildNodes
		{
			get
			{
				throw new global::System.NotImplementedException("The member XmlNodeList XmlText.ChildNodes is not implemented in Uno.");
			}
		}

		public IXmlNode ParentNode => _owner.Wrap(_backingText.ParentNode);

		public IXmlNode PreviousSibling => (IXmlNode)_owner.Wrap(_backingText.PreviousSibling);

		public string InnerText
		{
			get
			{
				throw new global::System.NotImplementedException("The member string XmlText.InnerText is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Xml.Dom.XmlText", "string XmlText.InnerText");
			}
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public global::Windows.Data.Xml.Dom.IXmlText SplitText(uint offset)
		{
			throw new global::System.NotImplementedException("The member IXmlText XmlText.SplitText(uint offset) is not implemented in Uno.");
		}
#endif
		// Forced skipping of method Windows.Data.Xml.Dom.XmlText.Data.get
		// Forced skipping of method Windows.Data.Xml.Dom.XmlText.Data.set
		// Forced skipping of method Windows.Data.Xml.Dom.XmlText.Length.get
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public string SubstringData(uint offset, uint count)
		{
			throw new global::System.NotImplementedException("The member string XmlText.SubstringData(uint offset, uint count) is not implemented in Uno.");
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public void AppendData(string data)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Xml.Dom.XmlText", "void XmlText.AppendData(string data)");
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public void InsertData(uint offset, string data)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Xml.Dom.XmlText", "void XmlText.InsertData(uint offset, string data)");
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public void DeleteData(uint offset, uint count)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Xml.Dom.XmlText", "void XmlText.DeleteData(uint offset, uint count)");
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public void ReplaceData(uint offset, uint count, string data)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Xml.Dom.XmlText", "void XmlText.ReplaceData(uint offset, uint count, string data)");
		}
#endif
		// Forced skipping of method Windows.Data.Xml.Dom.XmlText.NodeValue.get
		// Forced skipping of method Windows.Data.Xml.Dom.XmlText.NodeValue.set
		// Forced skipping of method Windows.Data.Xml.Dom.XmlText.NodeType.get
		// Forced skipping of method Windows.Data.Xml.Dom.XmlText.NodeName.get
		// Forced skipping of method Windows.Data.Xml.Dom.XmlText.ParentNode.get
		// Forced skipping of method Windows.Data.Xml.Dom.XmlText.ChildNodes.get
		// Forced skipping of method Windows.Data.Xml.Dom.XmlText.FirstChild.get
		// Forced skipping of method Windows.Data.Xml.Dom.XmlText.LastChild.get
		// Forced skipping of method Windows.Data.Xml.Dom.XmlText.PreviousSibling.get
		// Forced skipping of method Windows.Data.Xml.Dom.XmlText.NextSibling.get
		// Forced skipping of method Windows.Data.Xml.Dom.XmlText.Attributes.get
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public bool HasChildNodes()
		{
			throw new global::System.NotImplementedException("The member bool XmlText.HasChildNodes() is not implemented in Uno.");
		}
#endif
		// Forced skipping of method Windows.Data.Xml.Dom.XmlText.OwnerDocument.get
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public global::Windows.Data.Xml.Dom.IXmlNode InsertBefore(global::Windows.Data.Xml.Dom.IXmlNode newChild, global::Windows.Data.Xml.Dom.IXmlNode referenceChild)
		{
			throw new global::System.NotImplementedException("The member IXmlNode XmlText.InsertBefore(IXmlNode newChild, IXmlNode referenceChild) is not implemented in Uno.");
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public global::Windows.Data.Xml.Dom.IXmlNode ReplaceChild(global::Windows.Data.Xml.Dom.IXmlNode newChild, global::Windows.Data.Xml.Dom.IXmlNode referenceChild)
		{
			throw new global::System.NotImplementedException("The member IXmlNode XmlText.ReplaceChild(IXmlNode newChild, IXmlNode referenceChild) is not implemented in Uno.");
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public global::Windows.Data.Xml.Dom.IXmlNode RemoveChild(global::Windows.Data.Xml.Dom.IXmlNode childNode)
		{
			throw new global::System.NotImplementedException("The member IXmlNode XmlText.RemoveChild(IXmlNode childNode) is not implemented in Uno.");
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public global::Windows.Data.Xml.Dom.IXmlNode AppendChild(global::Windows.Data.Xml.Dom.IXmlNode newChild)
		{
			throw new global::System.NotImplementedException("The member IXmlNode XmlText.AppendChild(IXmlNode newChild) is not implemented in Uno.");
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public global::Windows.Data.Xml.Dom.IXmlNode CloneNode(bool deep)
		{
			throw new global::System.NotImplementedException("The member IXmlNode XmlText.CloneNode(bool deep) is not implemented in Uno.");
		}
#endif
		// Forced skipping of method Windows.Data.Xml.Dom.XmlText.NamespaceUri.get
		// Forced skipping of method Windows.Data.Xml.Dom.XmlText.LocalName.get
		// Forced skipping of method Windows.Data.Xml.Dom.XmlText.Prefix.get
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public void Normalize()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Xml.Dom.XmlText", "void XmlText.Normalize()");
		}
#endif
		// Forced skipping of method Windows.Data.Xml.Dom.XmlText.Prefix.set
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public global::Windows.Data.Xml.Dom.IXmlNode SelectSingleNode(string xpath)
		{
			throw new global::System.NotImplementedException("The member IXmlNode XmlText.SelectSingleNode(string xpath) is not implemented in Uno.");
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public global::Windows.Data.Xml.Dom.XmlNodeList SelectNodes(string xpath)
		{
			throw new global::System.NotImplementedException("The member XmlNodeList XmlText.SelectNodes(string xpath) is not implemented in Uno.");
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public global::Windows.Data.Xml.Dom.IXmlNode SelectSingleNodeNS(string xpath, object namespaces)
		{
			throw new global::System.NotImplementedException("The member IXmlNode XmlText.SelectSingleNodeNS(string xpath, object namespaces) is not implemented in Uno.");
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public global::Windows.Data.Xml.Dom.XmlNodeList SelectNodesNS(string xpath, object namespaces)
		{
			throw new global::System.NotImplementedException("The member XmlNodeList XmlText.SelectNodesNS(string xpath, object namespaces) is not implemented in Uno.");
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public string GetXml()
		{
			throw new global::System.NotImplementedException("The member string XmlText.GetXml() is not implemented in Uno.");
		}
#endif
		// Forced skipping of method Windows.Data.Xml.Dom.XmlText.InnerText.get
		// Forced skipping of method Windows.Data.Xml.Dom.XmlText.InnerText.set
		// Processing: Windows.Data.Xml.Dom.IXmlText
		// Processing: Windows.Data.Xml.Dom.IXmlCharacterData
		// Processing: Windows.Data.Xml.Dom.IXmlNode
		// Processing: Windows.Data.Xml.Dom.IXmlNodeSelector
		// Processing: Windows.Data.Xml.Dom.IXmlNodeSerializer
	}
}
