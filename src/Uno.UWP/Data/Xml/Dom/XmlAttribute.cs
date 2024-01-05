using SystemXmlAttribute = System.Xml.XmlAttribute;
using SystemXmlNode = System.Xml.XmlNode;

namespace Windows.Data.Xml.Dom
{
	public partial class XmlAttribute : IXmlNode, IXmlNodeSerializer, IXmlNodeSelector
	{
		private readonly XmlDocument _owner;
		internal readonly SystemXmlAttribute _backingAttribute;

		public XmlAttribute(XmlDocument owner, SystemXmlAttribute backingAttribute)
		{
			_owner = owner;
			_backingAttribute = backingAttribute;
		}

		public string Value
		{
			get => _backingAttribute.Value;
			set => _backingAttribute.Value = value;
		}

		public bool Specified => _backingAttribute.Specified;

		public string Name => _backingAttribute.Name;

		public object Prefix
		{
			get => _backingAttribute.Prefix;
			set => _backingAttribute.Prefix = value.ToString()!;
		}

		public object? NodeValue
		{
			get => _backingAttribute.Value;
			set => _backingAttribute.Value = value?.ToString();
		}

		public IXmlNode? FirstChild => (IXmlNode?)_owner.Wrap(_backingAttribute.FirstChild);

		public IXmlNode? LastChild => (IXmlNode?)_owner.Wrap(_backingAttribute.LastChild);

		public object LocalName => _backingAttribute.LocalName;

		public object NamespaceUri => _backingAttribute.NamespaceURI;

		public IXmlNode? NextSibling => (IXmlNode?)_owner.Wrap(_backingAttribute.NextSibling);

		public string NodeName => _owner.NodeName;

		public NodeType NodeType => Dom.NodeType.AttributeNode;

		public XmlNamedNodeMap? Attributes => (XmlNamedNodeMap?)_owner.Wrap(_backingAttribute.Attributes);

		public XmlDocument OwnerDocument => _owner;

		public XmlNodeList ChildNodes => (XmlNodeList)_owner.Wrap(_backingAttribute.ChildNodes);

		public IXmlNode? ParentNode => (IXmlNode?)_owner.Wrap(_backingAttribute.ParentNode);

		public IXmlNode? PreviousSibling => (IXmlNode?)_owner.Wrap(_backingAttribute.PreviousSibling);

		public string InnerText
		{
			get => _backingAttribute.InnerText;
			set => _backingAttribute.InnerText = value;
		}

		public bool HasChildNodes() => _backingAttribute.HasChildNodes;

		public IXmlNode? InsertBefore(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode?)_owner.Wrap(
				_backingAttribute.InsertBefore(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode ReplaceChild(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode)_owner.Wrap(
				_backingAttribute.ReplaceChild(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode RemoveChild(IXmlNode childNode) =>
			(IXmlNode)_owner.Wrap(
				_backingAttribute.RemoveChild(
					(SystemXmlNode)_owner.Unwrap(childNode)));

		public IXmlNode? AppendChild(IXmlNode newChild) =>
			(IXmlNode?)_owner.Wrap(
				_backingAttribute.AppendChild(
					(SystemXmlNode)_owner.Unwrap(newChild)));

		public IXmlNode CloneNode(bool deep) => (IXmlNode)_owner.Wrap(_backingAttribute.CloneNode(deep));

		public void Normalize() => _backingAttribute.Normalize();

		public IXmlNode? SelectSingleNode(string xpath) => (IXmlNode?)_owner.Wrap(_backingAttribute.SelectSingleNode(xpath));

		public XmlNodeList? SelectNodes(string xpath) => (XmlNodeList?)_owner.Wrap(_backingAttribute.SelectNodes(xpath));

		public string GetXml() => _backingAttribute.OuterXml;
	}
}
