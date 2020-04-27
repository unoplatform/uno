using System.Collections.Generic;
using SystemXmlNamedNodeMap = System.Xml.XmlNamedNodeMap;
using SystemXmlNode = System.Xml.XmlNode;

namespace Windows.Data.Xml.Dom
{
	public partial class XmlNamedNodeMap : IReadOnlyList<IXmlNode>, IEnumerable<IXmlNode>
	{
		private readonly XmlDocument _owner;
		internal readonly SystemXmlNamedNodeMap _backingNamedNodeMap;

		public XmlNamedNodeMap(XmlDocument owner, SystemXmlNamedNodeMap backingNamedNodeMap)
		{
			_owner = owner;
			_backingNamedNodeMap = backingNamedNodeMap;
		}

		public uint Length => (uint)_backingNamedNodeMap.Count;

		public uint Size => (uint)_backingNamedNodeMap.Count;

		public IXmlNode GetNamedItem(string name) => (IXmlNode)_owner.Wrap(_backingNamedNodeMap.GetNamedItem(name));

		public IXmlNode SetNamedItem(IXmlNode node) =>
			(IXmlNode)_owner.Wrap(
				_backingNamedNodeMap.SetNamedItem(
					(SystemXmlNode)_owner.Unwrap(node)));

		public IXmlNode RemoveNamedItem(string name) => (IXmlNode)_owner.Wrap(_backingNamedNodeMap.RemoveNamedItem(name));

		public IXmlNode this[int index]
		{
			get => (IXmlNode)_owner.Wrap(_backingNamedNodeMap.Item(index));
			set
			{
				throw new global::System.NotSupportedException();
			}
		}
		public IEnumerator<IXmlNode> GetEnumerator()
		{
			throw new global::System.NotSupportedException();
		}
		global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
		{
			throw new global::System.NotSupportedException();
		}
		public int Count
		{
			get
			{
				throw new global::System.NotSupportedException();
			}
			set
			{
				throw new global::System.NotSupportedException();
			}
		}
	}
}
