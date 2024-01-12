using SystemXmlDocumentFragment = System.Xml.XmlDocumentFragment;
using SystemXmlNode = System.Xml.XmlNode;

namespace Windows.Data.Xml.Dom
{
	public partial class XmlDocumentFragment : IXmlNode, IXmlNodeSerializer, IXmlNodeSelector
	{
		private readonly XmlDocument _owner;
		internal readonly SystemXmlDocumentFragment _backingDocumentFragment;

		internal XmlDocumentFragment(XmlDocument owner, SystemXmlDocumentFragment backingDocumentFragment)
		{
			_owner = owner;
			_backingDocumentFragment = backingDocumentFragment;
		}

		public object? Prefix
		{
			get => _backingDocumentFragment.Prefix;
			set => _backingDocumentFragment.Prefix = value?.ToString()!;
		}

		public object? NodeValue
		{
			get => _backingDocumentFragment.Value;
			set => _backingDocumentFragment.Value = value?.ToString();
		}

		public IXmlNode? FirstChild => (IXmlNode?)_owner.Wrap(_backingDocumentFragment.FirstChild);

		public IXmlNode? LastChild => (IXmlNode?)_owner.Wrap(_backingDocumentFragment.LastChild);

		public object LocalName => _backingDocumentFragment.LocalName;

		public IXmlNode? NextSibling => (IXmlNode?)_owner.Wrap(_backingDocumentFragment.NextSibling);

		public object NamespaceUri => _backingDocumentFragment.NamespaceURI;

		public Dom.NodeType NodeType => Dom.NodeType.DocumentFragmentNode;

		public string NodeName => _backingDocumentFragment.Name;

		public XmlNamedNodeMap? Attributes => (XmlNamedNodeMap?)_owner.Wrap(_backingDocumentFragment.Attributes);

		public XmlDocument OwnerDocument => _owner;

		public IXmlNode? ParentNode => (IXmlNode?)_owner.Wrap(_backingDocumentFragment.ParentNode);

		public XmlNodeList ChildNodes => (XmlNodeList)_owner.Wrap(_backingDocumentFragment.ChildNodes);

		public IXmlNode? PreviousSibling => (IXmlNode?)_owner.Wrap(_backingDocumentFragment.PreviousSibling);

		public string InnerText
		{
			get => _backingDocumentFragment.InnerText;
			set => _backingDocumentFragment.InnerText = value;
		}

		public bool HasChildNodes() => _backingDocumentFragment.HasChildNodes;

		public IXmlNode? InsertBefore(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode?)_owner.Wrap(
				_backingDocumentFragment.InsertBefore(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode ReplaceChild(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode)_owner.Wrap(
				_backingDocumentFragment.ReplaceChild(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode RemoveChild(IXmlNode childNode) =>
			(IXmlNode)_owner.Wrap(
				_backingDocumentFragment.RemoveChild(
					(SystemXmlNode)_owner.Unwrap(childNode)));

		public IXmlNode? AppendChild(IXmlNode newChild) =>
			(IXmlNode?)_owner.Wrap(
				_backingDocumentFragment.AppendChild(
					(SystemXmlNode)_owner.Unwrap(newChild)));

		public IXmlNode CloneNode(bool deep) => (IXmlNode)_owner.Wrap(_backingDocumentFragment.CloneNode(deep));

		public void Normalize() => _backingDocumentFragment.Normalize();

		public IXmlNode? SelectSingleNode(string xpath) => (IXmlNode?)_owner.Wrap(_backingDocumentFragment.SelectSingleNode(xpath));

		public XmlNodeList? SelectNodes(string xpath) => (XmlNodeList?)_owner.Wrap(_backingDocumentFragment.SelectNodes(xpath));

		public string GetXml() => _backingDocumentFragment.OuterXml;
	}
}
