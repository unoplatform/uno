using SystemXmlCDataSection = System.Xml.XmlCDataSection;
using SystemXmlNode = System.Xml.XmlNode;

namespace Windows.Data.Xml.Dom
{
	public partial class XmlCDataSection : IXmlText, IXmlCharacterData, IXmlNode, IXmlNodeSerializer, IXmlNodeSelector
	{
		private readonly XmlDocument _owner;
		internal readonly SystemXmlCDataSection _backingElement;

		internal XmlCDataSection(XmlDocument owner, SystemXmlCDataSection backingElement)
		{
			_owner = owner;
			_backingElement = backingElement;
		}

		public string Data
		{
			get => _backingElement.Data;
			set => _backingElement.Data = value;
		}

		public uint Length => (uint)_backingElement.Length;

		public object Prefix
		{
			get => _backingElement.Prefix;
			set => _backingElement.Prefix = value?.ToString();
		}

		public object NodeValue
		{
			get => _backingElement.Value;
			set => _backingElement.Value = value?.ToString();
		}

		public IXmlNode FirstChild => (IXmlNode)_owner.Wrap(_backingElement.FirstChild);

		public IXmlNode LastChild => (IXmlNode)_owner.Wrap(_backingElement.LastChild);

		public object LocalName => _owner.LocalName;

		public IXmlNode NextSibling => (IXmlNode)_owner.Wrap(_backingElement.NextSibling);

		public object NamespaceUri => _backingElement.NamespaceURI;

		public Dom.NodeType NodeType => Dom.NodeType.DataSectionNode;

		public string NodeName => _backingElement.Name;

		public XmlNamedNodeMap Attributes => (XmlNamedNodeMap)_owner.Wrap(_backingElement.Attributes);

		public XmlDocument OwnerDocument => _owner;

		public IXmlNode ParentNode => (IXmlNode)_owner.Wrap(_backingElement.ParentNode);

		public XmlNodeList ChildNodes => (XmlNodeList)_owner.Wrap(_backingElement.ChildNodes);

		public IXmlNode PreviousSibling => (IXmlNode)_owner.Wrap(_backingElement.PreviousSibling);

		public string InnerText
		{
			get => _backingElement.InnerText;
			set => _backingElement.InnerText = value;
		}

		public string SubstringData(uint offset, uint count) => _backingElement.Substring((int)offset, (int)count);

		public void AppendData(string data) => _backingElement.AppendData(data);

		public void InsertData(uint offset, string data) => _backingElement.InsertData((int)offset, data);

		public void DeleteData(uint offset, uint count) => _backingElement.DeleteData((int)offset, (int)count);

		public void ReplaceData(uint offset, uint count, string data) => _backingElement.ReplaceData((int)offset, (int)count, data);

		public bool HasChildNodes() => _backingElement.HasChildNodes;

		public IXmlNode InsertBefore(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode)_owner.Wrap(
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

		public IXmlNode AppendChild(IXmlNode newChild) =>
			(IXmlNode)_owner.Wrap(
				_backingElement.AppendChild(
					(SystemXmlNode)_owner.Unwrap(newChild)));

		public IXmlNode CloneNode(bool deep) => (IXmlNode)_owner.Wrap(_backingElement.CloneNode(deep));

		public void Normalize() => _backingElement.Normalize();

		public IXmlNode SelectSingleNode(string xpath) => (IXmlNode)_owner.Wrap(_backingElement.SelectSingleNode(xpath));

		public XmlNodeList SelectNodes(string xpath) => (XmlNodeList)_owner.Wrap(_backingElement.SelectNodes(xpath));

		public string GetXml() => _backingElement.OuterXml;
	}
}
