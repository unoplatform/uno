using SystemXmlComment = System.Xml.XmlComment;

namespace Windows.Data.Xml.Dom
{
	public partial class XmlComment : IXmlCharacterData, IXmlNode, IXmlNodeSerializer, IXmlNodeSelector
	{
		private readonly XmlDocument _owner;
		internal readonly SystemXmlComment _backingComment;

		public XmlComment(XmlDocument owner, SystemXmlComment backingComment)
		{
			_owner = owner;
			_backingComment = backingComment;
		}

		public string Data 
		{
			get => _backingComment.Data;
			set => _backingComment.Data = value;
		}

		public uint Length => _backingComment.Length;

		public object Prefix
		{
			get => _backingComment.Prefix;
			set => _backingComment.Prefix = value;
		}

		public object NodeValue
		{
			get => _backingComment.Value;
			set => _backingComment.Value = value?.ToString();
		}

		public IXmlNode FirstChild => _owner.Wrap(_backingComment.FirstChild);

		public IXmlNode LastChild => _owner.Wrap(_backingComment.LastChild);

		public object LocalName => _backingComment.LocalName;

		public object NamespaceUri => _backingComment.NamespaceURI;

		public IXmlNode NextSibling => _owner.Wrap(_backingComment.NextSibling);

		public string NodeName => _backingComment.Name;

		public Dom.NodeType NodeType => Dom.NodeType.CommentNode;

		public global::Windows.Data.Xml.Dom.XmlNamedNodeMap Attributes
		{
			get
			{
				throw new global::System.NotImplementedException("The member XmlNamedNodeMap XmlComment.Attributes is not implemented in Uno.");
			}
		}

		public XmlDocument OwnerDocument => _owner;

		public global::Windows.Data.Xml.Dom.XmlNodeList ChildNodes
		{
			get
			{
				throw new global::System.NotImplementedException("The member XmlNodeList XmlComment.ChildNodes is not implemented in Uno.");
			}
		}

		public IXmlNode ParentNode => _owner.Wrap(_backingComment.ParentNode);

		public IXmlNode PreviousSibling => _owner.Wrap(_backingComment.PreviousSibling);

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

		public IXmlNode InsertBefore(IXmlNode newChild, IXmlNode referenceChild) =>
			_owner.Wrap(_backingComment.InsertBefore(
				_owner.Unwrap(newChild),
				_owner.Unwrap(referenceChild)));

		public IXmlNode ReplaceChild(IXmlNode newChild, IXmlNode referenceChild) =>
			_owner.Wrap(
				_backingComment.ReplaceChild(
					_owner.Unwrap(newChild),
					_owner.Unwrap(referenceChild))); 

		public IXmlNode RemoveChild(IXmlNode childNode) => _owner.Wrap(_backingComment.RemoveChild(childNode));

		public IXmlNode AppendChild(IXmlNode newChild) =>
			_owner.Wrap(
				_backingComment.AppendChild(
					_owner.Unwrap(newChild)));

		public IXmlNode CloneNode(bool deep) => _owner.Wrap(_backingComment.CloneNode(deep));

		public void Normalize() => _backingComment.Normalize();

#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public global::Windows.Data.Xml.Dom.IXmlNode SelectSingleNode(string xpath)
		{
			throw new global::System.NotImplementedException("The member IXmlNode XmlComment.SelectSingleNode(string xpath) is not implemented in Uno.");
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public global::Windows.Data.Xml.Dom.XmlNodeList SelectNodes(string xpath)
		{
			throw new global::System.NotImplementedException("The member XmlNodeList XmlComment.SelectNodes(string xpath) is not implemented in Uno.");
		}
#endif

		public string GetXml() => _backingComment.OuterXml;
	}
}
