using SystemXmlDocumentType = System.Xml.XmlDocumentType;
using SystemXmlNode = System.Xml.XmlNode;

namespace Windows.Data.Xml.Dom
{
	public partial class XmlDocumentType : IXmlNode, IXmlNodeSerializer, IXmlNodeSelector
	{
		private readonly XmlDocument _owner;
		internal readonly SystemXmlDocumentType _backingDocumentType;

		internal XmlDocumentType(XmlDocument owner, SystemXmlDocumentType backingDocumentType)
		{
			_owner = owner;
			_backingDocumentType = backingDocumentType;
		}

		public XmlNamedNodeMap Entities => (XmlNamedNodeMap)_owner.Wrap(_backingDocumentType.Entities);

		public string Name => _backingDocumentType.Name;

		public XmlNamedNodeMap Notations => (XmlNamedNodeMap)_owner.Wrap(_backingDocumentType.Notations);

		public object Prefix
		{
			get => _backingDocumentType.Prefix;
			set => _backingDocumentType.Prefix = value.ToString()!;
		}

		public object? NodeValue
		{
			get => _backingDocumentType.Value;
			set => _backingDocumentType.Value = value?.ToString();
		}

		public IXmlNode? FirstChild => (IXmlNode?)_owner.Wrap(_backingDocumentType.FirstChild);

		public IXmlNode? LastChild => (IXmlNode?)_owner.Wrap(_backingDocumentType.LastChild);

		public object LocalName => _backingDocumentType.LocalName;

		public IXmlNode? NextSibling => (IXmlNode?)_owner.Wrap(_backingDocumentType.NextSibling);

		public object NamespaceUri => _backingDocumentType.NamespaceURI;

		public Dom.NodeType NodeType => Dom.NodeType.DocumentTypeNode;

		public string NodeName => _backingDocumentType.Name;

		public XmlNamedNodeMap? Attributes => (XmlNamedNodeMap?)_owner.Wrap(_backingDocumentType.Attributes);

		public XmlDocument OwnerDocument => _owner;

		public IXmlNode? ParentNode => (IXmlNode?)_owner.Wrap(_backingDocumentType.ParentNode);

		public XmlNodeList ChildNodes => (XmlNodeList)_owner.Wrap(_backingDocumentType.ChildNodes);

		public IXmlNode? PreviousSibling => (IXmlNode?)_owner.Wrap(_backingDocumentType.PreviousSibling);

		public string InnerText
		{
			get => _backingDocumentType.InnerText;
			set => _backingDocumentType.InnerText = value;
		}

		public bool HasChildNodes() => _backingDocumentType.HasChildNodes;

		public IXmlNode? InsertBefore(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode?)_owner.Wrap(
				_backingDocumentType.InsertBefore(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode ReplaceChild(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode)_owner.Wrap(
				_backingDocumentType.ReplaceChild(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode RemoveChild(IXmlNode childNode) =>
			(IXmlNode)_owner.Wrap(
				_backingDocumentType.RemoveChild(
					(SystemXmlNode)_owner.Unwrap(childNode)));

		public IXmlNode? AppendChild(IXmlNode newChild) =>
			(IXmlNode?)_owner.Wrap(
				_backingDocumentType.AppendChild(
					(SystemXmlNode)_owner.Unwrap(newChild)));

		public IXmlNode CloneNode(bool deep) => (IXmlNode)_owner.Wrap(_backingDocumentType.CloneNode(deep));

		public void Normalize() => _backingDocumentType.Normalize();

		public IXmlNode? SelectSingleNode(string xpath) => (IXmlNode?)_owner.Wrap(_backingDocumentType.SelectSingleNode(xpath));

		public XmlNodeList? SelectNodes(string xpath) => (XmlNodeList?)_owner.Wrap(_backingDocumentType.SelectNodes(xpath));

		public string GetXml() => _backingDocumentType.OuterXml;
	}
}
