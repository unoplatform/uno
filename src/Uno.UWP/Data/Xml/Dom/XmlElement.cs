using SystemXmlElement = System.Xml.XmlElement;

namespace Windows.Data.Xml.Dom
{
	public partial class XmlElement : IXmlNode, IXmlNodeSerializer, IXmlNodeSelector
	{
		private readonly XmlDocument _owner;
		private readonly SystemXmlElement _backingElement;

		internal XmlElement(XmlDocument owner, SystemXmlElement backingElement)
		{
			_owner = owner;
			_backingElement = backingElement;
		}

		public void SetAttribute(string attributeName, string attributeValue) =>
			_backingElement.SetAttribute(attributeName, attributeValue);

		public string TagName => _backingElement.Name;

		public object Prefix
		{
			get => _backingElement.Prefix;
			set => _backingElement.Prefix = value?.ToString();
		}

		public object NodeValue
		{
			get
			{
				throw new global::System.NotImplementedException("The member object XmlElement.NodeValue is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Xml.Dom.XmlElement", "object XmlElement.NodeValue");
			}
		}

		public IXmlNode FirstChild => (IXmlNode)_owner.Wrap(_backingElement.FirstChild);

		public IXmlNode LastChild => (IXmlNode)_owner.Wrap(_backingElement.LastChild);

		public object LocalName => _owner.LocalName;

		public IXmlNode NextSibling => (IXmlNode)_owner.Wrap(_backingElement.NextSibling);

		public object NamespaceUri => _backingElement.NamespaceURI;

		public Dom.NodeType NodeType => Dom.NodeType.ElementNode;

		public string NodeName => _backingElement.Name;

		public XmlNamedNodeMap Attributes
		{
			get
			{
				throw new global::System.NotImplementedException("The member XmlNamedNodeMap XmlElement.Attributes is not implemented in Uno.");
			}
		}

		public XmlDocument OwnerDocument => _owner;

		public IXmlNode ParentNode => (IXmlNode)_owner.Wrap(_backingElement.ParentNode);

		public XmlNodeList ChildNodes
		{
			get
			{
				throw new global::System.NotImplementedException("The member XmlNodeList XmlElement.ChildNodes is not implemented in Uno.");
			}
		}

		public IXmlNode PreviousSibling
		{
			get
			{
				throw new global::System.NotImplementedException("The member IXmlNode XmlElement.PreviousSibling is not implemented in Uno.");
			}
		}

		public string InnerText
		{
			get => _backingElement.InnerText;
			set => _backingElement.InnerText;
		}

		public string GetAttribute(string attributeName) => _backingElement.GetAttribute(attributeName);

		public void SetAttribute(string attributeName, string attributeValue) => _backingElement.SetAttribute(attributeName, attributeValue);

		public void RemoveAttribute(string attributeName) => _backingElement.RemoveAttribute(attributeName);

		public XmlAttribute GetAttributeNode(string attributeName) => (XmlAttribute)_owner.Wrap(_backingElement.GetAttributeNode(attributeName));

		public XmlAttribute SetAttributeNode(XmlAttribute newAttribute) =>
			(XmlAttribute)_owner.Wrap(
				_backingElement.SetAttributeNode(
					_owner.Unwrap(newAttribute));

		public XmlAttribute RemoveAttributeNode(XmlAttribute attributeNode) =>
			_owner.Wrap(
				_backingElement.RemoveAttributeNode(
					_owner.Unwrap(attributeNode)));

		public XmlNodeList GetElementsByTagName(string tagName) => _owner.Wrap(_backingElement.GetElementsByTagName(tagName));
		{
			throw new global::System.NotImplementedException("The member XmlNodeList XmlElement.GetElementsByTagName(string tagName) is not implemented in Uno.");
		}

#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[Uno.NotImplemented]
		public void SetAttributeNS(object namespaceUri, string qualifiedName, string value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Xml.Dom.XmlElement", "void XmlElement.SetAttributeNS(object namespaceUri, string qualifiedName, string value)");
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[Uno.NotImplemented]
		public string GetAttributeNS(object namespaceUri, string localName)
		{
			throw new global::System.NotImplementedException("The member string XmlElement.GetAttributeNS(object namespaceUri, string localName) is not implemented in Uno.");
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[Uno.NotImplemented]
		public void RemoveAttributeNS(object namespaceUri, string localName)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Xml.Dom.XmlElement", "void XmlElement.RemoveAttributeNS(object namespaceUri, string localName)");
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[Uno.NotImplemented]
		public XmlAttribute SetAttributeNodeNS(XmlAttribute newAttribute)
		{
			throw new global::System.NotImplementedException("The member XmlAttribute XmlElement.SetAttributeNodeNS(XmlAttribute newAttribute) is not implemented in Uno.");
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[Uno.NotImplemented]
		public XmlAttribute GetAttributeNodeNS(object namespaceUri, string localName)
		{
			throw new global::System.NotImplementedException("The member XmlAttribute XmlElement.GetAttributeNodeNS(object namespaceUri, string localName) is not implemented in Uno.");
		}

		public bool HasChildNodes() => _backingElement.HasChildNodes;

		public IXmlNode InsertBefore(IXmlNode newChild, IXmlNode referenceChild) =>
			_owner.Wrap(
				_backingElement.InsertBefore(
					_owner.Unwrap(newChild),
					_owner.Unwrap(referenceChild)));

		public IXmlNode ReplaceChild(IXmlNode newChild, IXmlNode referenceChild) =>
			_owner.Wrap(
				_backingElement.ReplaceChild(newChild, referenceChild));

		public IXmlNode RemoveChild(IXmlNode childNode) =>
			_owner.Wrap(
				_backingElement.RemoveChild(_owner.Unwrap(childNode)));

		public IXmlNode AppendChild(IXmlNode newChild) =>
			_owner.Wrap(
				_backingElement.AppendChild(_owner.Unwrap(newChild));

		public IXmlNode CloneNode(bool deep) => _owner.Wrap(_backingElement.CloneNode);

		public void Normalize() => _backingElement.Normalize();

		public IXmlNode SelectSingleNode(string xpath) => _owner.Wrap(_backingElement.SelectSingleNode(xpath));

		public XmlNodeList SelectNodes(string xpath)
		{
			throw new global::System.NotImplementedException("The member XmlNodeList XmlElement.SelectNodes(string xpath) is not implemented in Uno.");
		}

		public string GetXml() => _backingElement.OuterXml;
	}
}
