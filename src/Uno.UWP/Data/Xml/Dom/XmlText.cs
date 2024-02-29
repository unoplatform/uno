using SystemXmlText = System.Xml.XmlText;
using SystemXmlNode = System.Xml.XmlNode;

namespace Windows.Data.Xml.Dom
{
	public partial class XmlText : IXmlText, IXmlCharacterData, IXmlNode, IXmlNodeSerializer, IXmlNodeSelector
	{
		private readonly XmlDocument _owner;
		private readonly SystemXmlText _backingText;

		internal XmlText(XmlDocument owner, SystemXmlText backingText)
		{
			_owner = owner;
			_backingText = backingText;
		}

		public string Data
		{
			get => _backingText.Data;
			set => _backingText.Data = value;
		}

		public uint Length => (uint)_backingText.Length;

		public object? Prefix
		{
			get => _backingText.Prefix;
			set => _backingText.Prefix = value?.ToString()!;
		}

		public object? NodeValue
		{
			get => _backingText.Data;
			set => _backingText.Data = value?.ToString();
		}
		public IXmlNode? FirstChild => (IXmlNode?)_owner.Wrap(_backingText.FirstChild);

		public IXmlNode? LastChild => (IXmlNode?)_owner.Wrap(_backingText.LastChild);

		public object LocalName => _backingText.LocalName;

		public object NamespaceUri => _backingText.NamespaceURI;

		public IXmlNode? NextSibling => (IXmlNode?)_backingText.NextSibling;

		public string NodeName => _backingText.Name;

		public Dom.NodeType NodeType => Dom.NodeType.TextNode;

		public XmlNamedNodeMap? Attributes => (XmlNamedNodeMap?)_owner.Wrap(_backingText.Attributes);

		public XmlDocument OwnerDocument => _owner;

		public XmlNodeList ChildNodes => (XmlNodeList)_owner.Wrap(_backingText.ChildNodes);

		public IXmlNode? ParentNode => (IXmlNode?)_owner.Wrap(_backingText.ParentNode);

		public IXmlNode? PreviousSibling => (IXmlNode?)_owner.Wrap(_backingText.PreviousSibling);

		public string InnerText
		{
			get => _backingText.InnerText;
			set => _backingText.InnerText = value;
		}

		public IXmlText SplitText(uint offset) => (IXmlText)_owner.Wrap(_backingText.SplitText((int)offset));

		public string SubstringData(uint offset, uint count) => _backingText.Substring((int)offset, (int)count);

		public void AppendData(string data) => _backingText.AppendData(data);

		public void InsertData(uint offset, string data) => _backingText.InsertData((int)offset, data);

		public void DeleteData(uint offset, uint count) => _backingText.DeleteData((int)offset, (int)count);

		public void ReplaceData(uint offset, uint count, string data) => _backingText.ReplaceData((int)offset, (int)count, data);

		public bool HasChildNodes() => _backingText.HasChildNodes;

		public IXmlNode? InsertBefore(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode?)_owner.Wrap(
				_backingText.InsertBefore(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode ReplaceChild(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode)_owner.Wrap(
				_backingText.ReplaceChild(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode RemoveChild(IXmlNode childNode) =>
			(IXmlNode)_owner.Wrap(
				_backingText.RemoveChild(
					(SystemXmlNode)_owner.Unwrap(childNode)));

		public IXmlNode? AppendChild(IXmlNode newChild) =>
			(IXmlNode?)_owner.Wrap(
				_backingText.AppendChild(
					(SystemXmlNode)_owner.Unwrap(newChild)));

		public IXmlNode CloneNode(bool deep) => (IXmlNode)_owner.Wrap(_backingText.CloneNode(deep));

		public void Normalize() => _backingText.Normalize();

		public IXmlNode? SelectSingleNode(string xpath) => (IXmlNode?)_owner.Wrap(_backingText.SelectSingleNode(xpath));

		public XmlNodeList? SelectNodes(string xpath) => (XmlNodeList?)_owner.Wrap(_backingText.SelectNodes(xpath));

		public string GetXml() => _backingText.OuterXml;
	}
}
