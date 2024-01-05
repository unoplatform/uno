using SystemXmlNotation = System.Xml.XmlNotation;
using SystemXmlNode = System.Xml.XmlNode;

namespace Windows.Data.Xml.Dom
{
	public partial class DtdNotation : IXmlNode, IXmlNodeSerializer, IXmlNodeSelector
	{
		private readonly XmlDocument _owner;
		internal readonly SystemXmlNotation _backingNotation;

		internal DtdNotation(XmlDocument owner, SystemXmlNotation backingNotation)
		{
			_owner = owner;
			_backingNotation = backingNotation;
		}

		public object? PublicId => _backingNotation.PublicId;

		public object? SystemId => _backingNotation.SystemId;

		public object Prefix
		{
			get => _backingNotation.Prefix;
			set => _backingNotation.Prefix = value.ToString()!;
		}

		public object? NodeValue
		{
			get => _backingNotation.Value;
			set => _backingNotation.Value = value?.ToString();
		}

		public IXmlNode? FirstChild => (IXmlNode?)_owner.Wrap(_backingNotation.FirstChild);

		public IXmlNode? LastChild => (IXmlNode?)_owner.Wrap(_backingNotation.LastChild);

		public object LocalName => _backingNotation.LocalName;

		public IXmlNode? NextSibling => (IXmlNode?)_owner.Wrap(_backingNotation.NextSibling);

		public object NamespaceUri => _backingNotation.NamespaceURI;

		public Dom.NodeType NodeType => Dom.NodeType.NotationNode;

		public string NodeName => _backingNotation.Name;

		public XmlNamedNodeMap? Attributes => (XmlNamedNodeMap?)_owner.Wrap(_backingNotation.Attributes);

		public XmlDocument OwnerDocument => _owner;

		public IXmlNode? ParentNode => (IXmlNode?)_owner.Wrap(_backingNotation.ParentNode);

		public XmlNodeList ChildNodes => (XmlNodeList)_owner.Wrap(_backingNotation.ChildNodes);

		public IXmlNode? PreviousSibling => (IXmlNode?)_owner.Wrap(_backingNotation.PreviousSibling);

		public string InnerText
		{
			get => _backingNotation.InnerText;
			set => _backingNotation.InnerText = value;
		}

		public bool HasChildNodes() => _backingNotation.HasChildNodes;

		public IXmlNode? InsertBefore(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode?)_owner.Wrap(
				_backingNotation.InsertBefore(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode ReplaceChild(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode)_owner.Wrap(
				_backingNotation.ReplaceChild(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode RemoveChild(IXmlNode childNode) =>
			(IXmlNode)_owner.Wrap(
				_backingNotation.RemoveChild(
					(SystemXmlNode)_owner.Unwrap(childNode)));

		public IXmlNode? AppendChild(IXmlNode newChild) =>
			(IXmlNode?)_owner.Wrap(
				_backingNotation.AppendChild(
					(SystemXmlNode)_owner.Unwrap(newChild)));

		public IXmlNode CloneNode(bool deep) => (IXmlNode)_owner.Wrap(_backingNotation.CloneNode(deep));

		public void Normalize() => _backingNotation.Normalize();

		public IXmlNode? SelectSingleNode(string xpath) => (IXmlNode?)_owner.Wrap(_backingNotation.SelectSingleNode(xpath));

		public XmlNodeList? SelectNodes(string xpath) => (XmlNodeList?)_owner.Wrap(_backingNotation.SelectNodes(xpath));

		public string GetXml() => _backingNotation.OuterXml;
	}
}
