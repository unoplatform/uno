using SystemXmlCDataSection = System.Xml.XmlCDataSection;
using SystemXmlNode = System.Xml.XmlNode;

namespace Windows.Data.Xml.Dom
{
	public partial class XmlCDataSection : IXmlText, IXmlCharacterData, IXmlNode, IXmlNodeSerializer, IXmlNodeSelector
	{
		private readonly XmlDocument _owner;
		internal readonly SystemXmlCDataSection _backingDataSection;

		internal XmlCDataSection(XmlDocument owner, SystemXmlCDataSection backingDataSection)
		{
			_owner = owner;
			_backingDataSection = backingDataSection;
		}

		public string Data
		{
			get => _backingDataSection.Data;
			set => _backingDataSection.Data = value;
		}

		public uint Length => (uint)_backingDataSection.Length;

		public object? Prefix
		{
			get => _backingDataSection.Prefix;
			set => _backingDataSection.Prefix = value?.ToString()!;
		}

		public object? NodeValue
		{
			get => _backingDataSection.Value;
			set => _backingDataSection.Value = value?.ToString();
		}

		public IXmlNode? FirstChild => (IXmlNode?)_owner.Wrap(_backingDataSection.FirstChild);

		public IXmlNode? LastChild => (IXmlNode?)_owner.Wrap(_backingDataSection.LastChild);

		public object LocalName => _backingDataSection.LocalName;

		public IXmlNode? NextSibling => (IXmlNode?)_owner.Wrap(_backingDataSection.NextSibling);

		public object NamespaceUri => _backingDataSection.NamespaceURI;

		public Dom.NodeType NodeType => Dom.NodeType.DataSectionNode;

		public string NodeName => _backingDataSection.Name;

		public XmlNamedNodeMap? Attributes => (XmlNamedNodeMap?)_owner.Wrap(_backingDataSection.Attributes);

		public XmlDocument OwnerDocument => _owner;

		public IXmlNode? ParentNode => (IXmlNode?)_owner.Wrap(_backingDataSection.ParentNode);

		public XmlNodeList ChildNodes => (XmlNodeList)_owner.Wrap(_backingDataSection.ChildNodes);

		public IXmlNode? PreviousSibling => (IXmlNode?)_owner.Wrap(_backingDataSection.PreviousSibling);

		public string InnerText
		{
			get => _backingDataSection.InnerText;
			set => _backingDataSection.InnerText = value;
		}

		public string SubstringData(uint offset, uint count) => _backingDataSection.Substring((int)offset, (int)count);

		public void AppendData(string data) => _backingDataSection.AppendData(data);

		public void InsertData(uint offset, string data) => _backingDataSection.InsertData((int)offset, data);

		public void DeleteData(uint offset, uint count) => _backingDataSection.DeleteData((int)offset, (int)count);

		public void ReplaceData(uint offset, uint count, string data) => _backingDataSection.ReplaceData((int)offset, (int)count, data);

		public bool HasChildNodes() => _backingDataSection.HasChildNodes;

		public IXmlNode? InsertBefore(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode?)_owner.Wrap(
				_backingDataSection.InsertBefore(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode ReplaceChild(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode)_owner.Wrap(
				_backingDataSection.ReplaceChild(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode RemoveChild(IXmlNode childNode) =>
			(IXmlNode)_owner.Wrap(
				_backingDataSection.RemoveChild(
					(SystemXmlNode)_owner.Unwrap(childNode)));

		public IXmlNode? AppendChild(IXmlNode newChild) =>
			(IXmlNode?)_owner.Wrap(
				_backingDataSection.AppendChild(
					(SystemXmlNode)_owner.Unwrap(newChild)));

		public IXmlNode CloneNode(bool deep) => (IXmlNode)_owner.Wrap(_backingDataSection.CloneNode(deep));

		public void Normalize() => _backingDataSection.Normalize();

		public IXmlNode? SelectSingleNode(string xpath) => (IXmlNode?)_owner.Wrap(_backingDataSection.SelectSingleNode(xpath));

		public XmlNodeList? SelectNodes(string xpath) => (XmlNodeList?)_owner.Wrap(_backingDataSection.SelectNodes(xpath));

		public string GetXml() => _backingDataSection.OuterXml;
	}
}
