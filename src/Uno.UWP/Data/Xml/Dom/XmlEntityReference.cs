using SystemXmlEntityReference = System.Xml.XmlEntityReference;
using SystemXmlNode = System.Xml.XmlNode;

namespace Windows.Data.Xml.Dom
{
	public partial class XmlEntityReference : IXmlNode, IXmlNodeSerializer, IXmlNodeSelector
	{
		private readonly XmlDocument _owner;
		internal readonly SystemXmlEntityReference _backingEntityReference;

		internal XmlEntityReference(XmlDocument owner, SystemXmlEntityReference backingEntityReference)
		{
			_owner = owner;
			_backingEntityReference = backingEntityReference;
		}

		public object? Prefix
		{
			get => _backingEntityReference.Prefix;
			set => _backingEntityReference.Prefix = value?.ToString()!;
		}

		public object? NodeValue
		{
			get => _backingEntityReference.Value;
			set => _backingEntityReference.Value = value?.ToString();
		}

		public IXmlNode? FirstChild => (IXmlNode?)_owner.Wrap(_backingEntityReference.FirstChild);

		public IXmlNode? LastChild => (IXmlNode?)_owner.Wrap(_backingEntityReference.LastChild);

		public object LocalName => _backingEntityReference.LocalName;

		public IXmlNode? NextSibling => (IXmlNode?)_owner.Wrap(_backingEntityReference.NextSibling);

		public object NamespaceUri => _backingEntityReference.NamespaceURI;

		public NodeType NodeType => Dom.NodeType.EntityReferenceNode;

		public string NodeName => _backingEntityReference.Name;

		public XmlNamedNodeMap? Attributes => (XmlNamedNodeMap?)_owner.Wrap(_backingEntityReference.Attributes);

		public XmlDocument OwnerDocument => _owner;

		public IXmlNode? ParentNode => (IXmlNode?)_owner.Wrap(_backingEntityReference.ParentNode);

		public XmlNodeList ChildNodes => (XmlNodeList)_owner.Wrap(_backingEntityReference.ChildNodes);

		public IXmlNode? PreviousSibling => (IXmlNode?)_owner.Wrap(_backingEntityReference.PreviousSibling);

		public string InnerText
		{
			get => _backingEntityReference.InnerText;
			set => _backingEntityReference.InnerText = value;
		}

		public bool HasChildNodes() => _backingEntityReference.HasChildNodes;

		public IXmlNode? InsertBefore(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode?)_owner.Wrap(
				_backingEntityReference.InsertBefore(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode ReplaceChild(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode)_owner.Wrap(
				_backingEntityReference.ReplaceChild(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode RemoveChild(IXmlNode childNode) =>
			(IXmlNode)_owner.Wrap(
				_backingEntityReference.RemoveChild(
					(SystemXmlNode)_owner.Unwrap(childNode)));

		public IXmlNode? AppendChild(IXmlNode newChild) =>
			(IXmlNode?)_owner.Wrap(
				_backingEntityReference.AppendChild(
					(SystemXmlNode)_owner.Unwrap(newChild)));

		public IXmlNode CloneNode(bool deep) => (IXmlNode)_owner.Wrap(_backingEntityReference.CloneNode(deep));

		public void Normalize() => _backingEntityReference.Normalize();

		public IXmlNode? SelectSingleNode(string xpath) => (IXmlNode?)_owner.Wrap(_backingEntityReference.SelectSingleNode(xpath));

		public XmlNodeList? SelectNodes(string xpath) => (XmlNodeList?)_owner.Wrap(_backingEntityReference.SelectNodes(xpath));

		public string GetXml() => _backingEntityReference.OuterXml;
	}
}
