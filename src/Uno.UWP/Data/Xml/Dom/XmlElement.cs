using System.Xml;
using SystemXmlElement = System.Xml.XmlElement;
using SystemXmlAttribute = System.Xml.XmlAttribute;
using SystemXmlNode = System.Xml.XmlNode;

namespace Windows.Data.Xml.Dom
{
	public partial class XmlElement : IXmlNode, IXmlNodeSerializer, IXmlNodeSelector
	{
		private readonly XmlDocument _owner;
		internal readonly SystemXmlElement _backingElement;

		internal XmlElement(XmlDocument owner, SystemXmlElement backingElement)
		{
			_owner = owner;
			_backingElement = backingElement;
		}

		public string TagName => _backingElement.Name;

		public object? Prefix
		{
			get => _backingElement.Prefix;
			set => _backingElement.Prefix = value?.ToString()!;
		}

		public object? NodeValue
		{
			get => _backingElement.Value;
			set => _backingElement.Value = value?.ToString();
		}

		public IXmlNode? FirstChild => (IXmlNode?)_owner.Wrap(_backingElement.FirstChild);

		public IXmlNode? LastChild => (IXmlNode?)_owner.Wrap(_backingElement.LastChild);

		public object LocalName => _backingElement.LocalName;

		public IXmlNode? NextSibling => (IXmlNode?)_owner.Wrap(_backingElement.NextSibling);

		public object NamespaceUri => _backingElement.NamespaceURI;

		public Dom.NodeType NodeType => Dom.NodeType.ElementNode;

		public string NodeName => _backingElement.Name;

		public XmlNamedNodeMap? Attributes => (XmlNamedNodeMap?)_owner.Wrap(_backingElement.Attributes);

		public XmlDocument OwnerDocument => _owner;

		public IXmlNode? ParentNode => (IXmlNode?)_owner.Wrap(_backingElement.ParentNode);

		public XmlNodeList ChildNodes => (XmlNodeList)_owner.Wrap(_backingElement.ChildNodes);

		public IXmlNode? PreviousSibling => (IXmlNode?)_owner.Wrap(_backingElement.PreviousSibling);

		public string InnerText
		{
			get => _backingElement.InnerText;
			set => _backingElement.InnerText = value;
		}

		public string GetAttribute(string attributeName) => _backingElement.GetAttribute(attributeName);

		public void SetAttribute(string attributeName, string attributeValue) => _backingElement.SetAttribute(attributeName, attributeValue);

		public void RemoveAttribute(string attributeName) => _backingElement.RemoveAttribute(attributeName);

		public XmlAttribute? GetAttributeNode(string attributeName) => (XmlAttribute?)_owner.Wrap(_backingElement.GetAttributeNode(attributeName));

		public XmlAttribute? SetAttributeNode(XmlAttribute newAttribute) =>
			(XmlAttribute?)_owner.Wrap(
				_backingElement.SetAttributeNode(
					(SystemXmlAttribute)_owner.Unwrap(newAttribute)));

		public XmlAttribute? RemoveAttributeNode(XmlAttribute attributeNode) =>
			(XmlAttribute?)_owner.Wrap(
				_backingElement.RemoveAttributeNode(
					(SystemXmlAttribute)_owner.Unwrap(attributeNode)));

		public XmlNodeList GetElementsByTagName(string tagName) => (XmlNodeList)_owner.Wrap(_backingElement.GetElementsByTagName(tagName));

		public bool HasChildNodes() => _backingElement.HasChildNodes;

		public IXmlNode? InsertBefore(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode?)_owner.Wrap(
				_backingElement.InsertBefore(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode ReplaceChild(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode)_owner.Wrap(
				_backingElement.ReplaceChild(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode RemoveChild(IXmlNode childNode) =>
			(IXmlNode)_owner.Wrap(
				_backingElement.RemoveChild(
					(SystemXmlNode)_owner.Unwrap(childNode)));

		public IXmlNode? AppendChild(IXmlNode newChild) =>
			(IXmlNode?)_owner.Wrap(
				_backingElement.AppendChild(
					(SystemXmlNode)_owner.Unwrap(newChild)));

		public IXmlNode CloneNode(bool deep) => (IXmlNode)_owner.Wrap(_backingElement.CloneNode(deep));

		public void Normalize() => _backingElement.Normalize();

		public IXmlNode? SelectSingleNode(string xpath) => (IXmlNode?)_owner.Wrap(_backingElement.SelectSingleNode(xpath));

		public XmlNodeList? SelectNodes(string xpath) => (XmlNodeList?)_owner.Wrap(_backingElement.SelectNodes(xpath));

		public string GetXml() => _backingElement.OuterXml;
	}
}
