
namespace Windows.Data.Xml.Dom
{

	public partial class DtdEntity : IXmlNode, IXmlNodeSerializer, IXmlNodeSelector
	{
		public object NotationName { get; }

		public object PublicId { get; }

		public object SystemId { get; }

		public object Prefix
		{
			get
			{
				throw new global::System.NotImplementedException("The member object DtdEntity.Prefix is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Xml.Dom.DtdEntity", "object DtdEntity.Prefix");
			}
		}
		public object NodeValue
		{
			get
			{
				throw new global::System.NotImplementedException("The member object DtdEntity.NodeValue is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Xml.Dom.DtdEntity", "object DtdEntity.NodeValue");
			}
		}
		public global::Windows.Data.Xml.Dom.IXmlNode FirstChild
		{
			get
			{
				throw new global::System.NotImplementedException("The member IXmlNode DtdEntity.FirstChild is not implemented in Uno.");
			}
		}
		public global::Windows.Data.Xml.Dom.IXmlNode LastChild
		{
			get
			{
				throw new global::System.NotImplementedException("The member IXmlNode DtdEntity.LastChild is not implemented in Uno.");
			}
		}
		public object LocalName
		{
			get
			{
				throw new global::System.NotImplementedException("The member object DtdEntity.LocalName is not implemented in Uno.");
			}
		}
		public object NamespaceUri
		{
			get
			{
				throw new global::System.NotImplementedException("The member object DtdEntity.NamespaceUri is not implemented in Uno.");
			}
		}
		public global::Windows.Data.Xml.Dom.IXmlNode NextSibling
		{
			get
			{
				throw new global::System.NotImplementedException("The member IXmlNode DtdEntity.NextSibling is not implemented in Uno.");
			}
		}
		public string NodeName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string DtdEntity.NodeName is not implemented in Uno.");
			}
		}
		public global::Windows.Data.Xml.Dom.NodeType NodeType
		{
			get
			{
				throw new global::System.NotImplementedException("The member NodeType DtdEntity.NodeType is not implemented in Uno.");
			}
		}
		public global::Windows.Data.Xml.Dom.XmlNamedNodeMap Attributes
		{
			get
			{
				throw new global::System.NotImplementedException("The member XmlNamedNodeMap DtdEntity.Attributes is not implemented in Uno.");
			}
		}
		public global::Windows.Data.Xml.Dom.XmlDocument OwnerDocument
		{
			get
			{
				throw new global::System.NotImplementedException("The member XmlDocument DtdEntity.OwnerDocument is not implemented in Uno.");
			}
		}
		public global::Windows.Data.Xml.Dom.XmlNodeList ChildNodes
		{
			get
			{
				throw new global::System.NotImplementedException("The member XmlNodeList DtdEntity.ChildNodes is not implemented in Uno.");
			}
		}
		public global::Windows.Data.Xml.Dom.IXmlNode ParentNode
		{
			get
			{
				throw new global::System.NotImplementedException("The member IXmlNode DtdEntity.ParentNode is not implemented in Uno.");
			}
		}
		public global::Windows.Data.Xml.Dom.IXmlNode PreviousSibling
		{
			get
			{
				throw new global::System.NotImplementedException("The member IXmlNode DtdEntity.PreviousSibling is not implemented in Uno.");
			}
		}
		public string InnerText
		{
			get
			{
				throw new global::System.NotImplementedException("The member string DtdEntity.InnerText is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Xml.Dom.DtdEntity", "string DtdEntity.InnerText");
			}
		}
#endif
		// Forced skipping of method Windows.Data.Xml.Dom.DtdEntity.PublicId.get
		// Forced skipping of method Windows.Data.Xml.Dom.DtdEntity.SystemId.get
		// Forced skipping of method Windows.Data.Xml.Dom.DtdEntity.NotationName.get
		// Forced skipping of method Windows.Data.Xml.Dom.DtdEntity.NodeValue.get
		// Forced skipping of method Windows.Data.Xml.Dom.DtdEntity.NodeValue.set
		// Forced skipping of method Windows.Data.Xml.Dom.DtdEntity.NodeType.get
		// Forced skipping of method Windows.Data.Xml.Dom.DtdEntity.NodeName.get
		// Forced skipping of method Windows.Data.Xml.Dom.DtdEntity.ParentNode.get
		// Forced skipping of method Windows.Data.Xml.Dom.DtdEntity.ChildNodes.get
		// Forced skipping of method Windows.Data.Xml.Dom.DtdEntity.FirstChild.get
		// Forced skipping of method Windows.Data.Xml.Dom.DtdEntity.LastChild.get
		// Forced skipping of method Windows.Data.Xml.Dom.DtdEntity.PreviousSibling.get
		// Forced skipping of method Windows.Data.Xml.Dom.DtdEntity.NextSibling.get
		// Forced skipping of method Windows.Data.Xml.Dom.DtdEntity.Attributes.get
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public bool HasChildNodes()
		{
			throw new global::System.NotImplementedException("The member bool DtdEntity.HasChildNodes() is not implemented in Uno.");
		}
#endif
		// Forced skipping of method Windows.Data.Xml.Dom.DtdEntity.OwnerDocument.get
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public global::Windows.Data.Xml.Dom.IXmlNode InsertBefore(global::Windows.Data.Xml.Dom.IXmlNode newChild, global::Windows.Data.Xml.Dom.IXmlNode referenceChild)
		{
			throw new global::System.NotImplementedException("The member IXmlNode DtdEntity.InsertBefore(IXmlNode newChild, IXmlNode referenceChild) is not implemented in Uno.");
		}
		public global::Windows.Data.Xml.Dom.IXmlNode ReplaceChild(global::Windows.Data.Xml.Dom.IXmlNode newChild, global::Windows.Data.Xml.Dom.IXmlNode referenceChild)
		{
			throw new global::System.NotImplementedException("The member IXmlNode DtdEntity.ReplaceChild(IXmlNode newChild, IXmlNode referenceChild) is not implemented in Uno.");
		}
		public global::Windows.Data.Xml.Dom.IXmlNode RemoveChild(global::Windows.Data.Xml.Dom.IXmlNode childNode)
		{
			throw new global::System.NotImplementedException("The member IXmlNode DtdEntity.RemoveChild(IXmlNode childNode) is not implemented in Uno.");
		}
		public global::Windows.Data.Xml.Dom.IXmlNode AppendChild(global::Windows.Data.Xml.Dom.IXmlNode newChild)
		{
			throw new global::System.NotImplementedException("The member IXmlNode DtdEntity.AppendChild(IXmlNode newChild) is not implemented in Uno.");
		}
		public global::Windows.Data.Xml.Dom.IXmlNode CloneNode(bool deep)
		{
			throw new global::System.NotImplementedException("The member IXmlNode DtdEntity.CloneNode(bool deep) is not implemented in Uno.");
		}
#endif
		// Forced skipping of method Windows.Data.Xml.Dom.DtdEntity.NamespaceUri.get
		// Forced skipping of method Windows.Data.Xml.Dom.DtdEntity.LocalName.get
		// Forced skipping of method Windows.Data.Xml.Dom.DtdEntity.Prefix.get
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public void Normalize()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Xml.Dom.DtdEntity", "void DtdEntity.Normalize()");
		}
#endif
		// Forced skipping of method Windows.Data.Xml.Dom.DtdEntity.Prefix.set
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public global::Windows.Data.Xml.Dom.IXmlNode SelectSingleNode(string xpath)
		{
			throw new global::System.NotImplementedException("The member IXmlNode DtdEntity.SelectSingleNode(string xpath) is not implemented in Uno.");
		}
		public global::Windows.Data.Xml.Dom.XmlNodeList SelectNodes(string xpath)
		{
			throw new global::System.NotImplementedException("The member XmlNodeList DtdEntity.SelectNodes(string xpath) is not implemented in Uno.");
		}
		public global::Windows.Data.Xml.Dom.IXmlNode SelectSingleNodeNS(string xpath, object namespaces)
		{
			throw new global::System.NotImplementedException("The member IXmlNode DtdEntity.SelectSingleNodeNS(string xpath, object namespaces) is not implemented in Uno.");
		}
		public global::Windows.Data.Xml.Dom.XmlNodeList SelectNodesNS(string xpath, object namespaces)
		{
			throw new global::System.NotImplementedException("The member XmlNodeList DtdEntity.SelectNodesNS(string xpath, object namespaces) is not implemented in Uno.");
		}
		public string GetXml()
		{
			throw new global::System.NotImplementedException("The member string DtdEntity.GetXml() is not implemented in Uno.");
		}
#endif
		// Forced skipping of method Windows.Data.Xml.Dom.DtdEntity.InnerText.get
		// Forced skipping of method Windows.Data.Xml.Dom.DtdEntity.InnerText.set
		// Processing: Windows.Data.Xml.Dom.IXmlNode
		// Processing: Windows.Data.Xml.Dom.IXmlNodeSelector
		// Processing: Windows.Data.Xml.Dom.IXmlNodeSerializer
	}
}
