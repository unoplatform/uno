using SystemXmlEntity = System.Xml.XmlEntity;
using SystemXmlNode = System.Xml.XmlNode;

namespace Windows.Data.Xml.Dom
{
	public partial class DtdEntity : IXmlNode, IXmlNodeSerializer, IXmlNodeSelector
	{
		private readonly XmlDocument _owner;
		internal readonly SystemXmlEntity _backingEntity;

		internal DtdEntity(XmlDocument owner, SystemXmlEntity backingEntity)
		{
			_owner = owner;
			_backingEntity = backingEntity;
		}

		public object? NotationName => _backingEntity.NotationName;

		public object? PublicId => _backingEntity.PublicId;

		public object? SystemId => _backingEntity.SystemId;

		public object? Prefix
		{
			get => _backingEntity.Prefix;
			set => _backingEntity.Prefix = value?.ToString()!;
		}

		public object? NodeValue
		{
			get => _backingEntity.Value;
			set => _backingEntity.Value = value?.ToString();
		}

		public IXmlNode? FirstChild => (IXmlNode?)_owner.Wrap(_backingEntity.FirstChild);

		public IXmlNode? LastChild => (IXmlNode?)_owner.Wrap(_backingEntity.LastChild);

		public object LocalName => _backingEntity.LocalName;

		public IXmlNode? NextSibling => (IXmlNode?)_owner.Wrap(_backingEntity.NextSibling);

		public object NamespaceUri => _backingEntity.NamespaceURI;

		public Dom.NodeType NodeType => Dom.NodeType.EntityNode;

		public string NodeName => _backingEntity.Name;

		public XmlNamedNodeMap? Attributes => (XmlNamedNodeMap?)_owner.Wrap(_backingEntity.Attributes);

		public XmlDocument OwnerDocument => _owner;

		public IXmlNode? ParentNode => (IXmlNode?)_owner.Wrap(_backingEntity.ParentNode);

		public XmlNodeList ChildNodes => (XmlNodeList)_owner.Wrap(_backingEntity.ChildNodes);

		public IXmlNode? PreviousSibling => (IXmlNode?)_owner.Wrap(_backingEntity.PreviousSibling);

		public string InnerText
		{
			get => _backingEntity.InnerText;
			set => _backingEntity.InnerText = value;
		}

		public bool HasChildNodes() => _backingEntity.HasChildNodes;

		public IXmlNode? InsertBefore(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode?)_owner.Wrap(
				_backingEntity.InsertBefore(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode ReplaceChild(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode)_owner.Wrap(
				_backingEntity.ReplaceChild(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode RemoveChild(IXmlNode childNode) =>
			(IXmlNode)_owner.Wrap(
				_backingEntity.RemoveChild(
					(SystemXmlNode)_owner.Unwrap(childNode)));

		public IXmlNode? AppendChild(IXmlNode newChild) =>
			(IXmlNode?)_owner.Wrap(
				_backingEntity.AppendChild(
					(SystemXmlNode)_owner.Unwrap(newChild)));

		public IXmlNode CloneNode(bool deep) => (IXmlNode)_owner.Wrap(_backingEntity.CloneNode(deep));

		public void Normalize() => _backingEntity.Normalize();

		public IXmlNode? SelectSingleNode(string xpath) => (IXmlNode?)_owner.Wrap(_backingEntity.SelectSingleNode(xpath));

		public XmlNodeList? SelectNodes(string xpath) => (XmlNodeList?)_owner.Wrap(_backingEntity.SelectNodes(xpath));

		public string GetXml() => _backingEntity.OuterXml;
	}
}
