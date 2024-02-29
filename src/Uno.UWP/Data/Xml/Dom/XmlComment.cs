using SystemXmlComment = System.Xml.XmlComment;
using SystemXmlNode = System.Xml.XmlNode;

namespace Windows.Data.Xml.Dom
{
	public partial class XmlComment : IXmlCharacterData, IXmlNode, IXmlNodeSerializer, IXmlNodeSelector
	{
		private readonly XmlDocument _owner;
		internal readonly SystemXmlComment _backingComment;

		internal XmlComment(XmlDocument owner, SystemXmlComment backingComment)
		{
			_owner = owner;
			_backingComment = backingComment;
		}

		public string Data
		{
			get => _backingComment.Data;
			set => _backingComment.Data = value;
		}

		public uint Length => (uint)_backingComment.Length;

		public object? Prefix
		{
			get => _backingComment.Prefix;
			set => _backingComment.Prefix = value?.ToString()!;
		}

		public object? NodeValue
		{
			get => _backingComment.Value;
			set => _backingComment.Value = value?.ToString();
		}

		public IXmlNode? FirstChild => (IXmlNode?)_owner.Wrap(_backingComment.FirstChild);

		public IXmlNode? LastChild => (IXmlNode?)_owner.Wrap(_backingComment.LastChild);

		public object LocalName => _backingComment.LocalName;

		public object NamespaceUri => _backingComment.NamespaceURI;

		public IXmlNode? NextSibling => (IXmlNode?)_owner.Wrap(_backingComment.NextSibling);

		public string NodeName => _backingComment.Name;

		public Dom.NodeType NodeType => Dom.NodeType.CommentNode;

		public XmlNamedNodeMap? Attributes => (XmlNamedNodeMap?)_owner.Wrap(_backingComment.Attributes);

		public XmlDocument OwnerDocument => _owner;

		public XmlNodeList ChildNodes => (XmlNodeList)_owner.Wrap(_backingComment.ChildNodes);

		public IXmlNode? ParentNode => (IXmlNode?)_owner.Wrap(_backingComment.ParentNode);

		public IXmlNode? PreviousSibling => (IXmlNode?)_owner.Wrap(_backingComment.PreviousSibling);

		public string InnerText
		{
			get => _backingComment.InnerText;
			set => _backingComment.InnerText = value;
		}

		public string SubstringData(uint offset, uint count) => _backingComment.Substring((int)offset, (int)count);

		public void AppendData(string data) => _backingComment.AppendData(data);

		public void InsertData(uint offset, string data) => _backingComment.InsertData((int)offset, data);

		public void DeleteData(uint offset, uint count) => _backingComment.DeleteData((int)offset, (int)count);

		public void ReplaceData(uint offset, uint count, string data) => _backingComment.ReplaceData((int)offset, (int)count, data);

		public bool HasChildNodes() => _backingComment.HasChildNodes;

		public IXmlNode? InsertBefore(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode?)_owner.Wrap(
				_backingComment.InsertBefore(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode ReplaceChild(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode)_owner.Wrap(
				_backingComment.ReplaceChild(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode RemoveChild(IXmlNode childNode) =>
			(IXmlNode)_owner.Wrap(
				_backingComment.RemoveChild(
					(SystemXmlNode)_owner.Unwrap(childNode)));

		public IXmlNode? AppendChild(IXmlNode newChild) =>
			(IXmlNode?)_owner.Wrap(
				_backingComment.AppendChild(
					(SystemXmlNode)_owner.Unwrap(newChild)));

		public IXmlNode CloneNode(bool deep) => (IXmlNode)_owner.Wrap(_backingComment.CloneNode(deep));

		public void Normalize() => _backingComment.Normalize();

		public IXmlNode? SelectSingleNode(string xpath) => (IXmlNode?)_owner.Wrap(_backingComment.SelectSingleNode(xpath));

		public XmlNodeList? SelectNodes(string xpath) => (XmlNodeList?)_owner.Wrap(_backingComment.SelectNodes(xpath));

		public string GetXml() => _backingComment.OuterXml;
	}
}
