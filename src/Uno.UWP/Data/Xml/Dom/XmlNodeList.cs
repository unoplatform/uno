using System.Collections.Generic;
using SystemXmlNodeList = System.Xml.XmlNodeList;

namespace Windows.Data.Xml.Dom
{
	public partial class XmlNodeList : IReadOnlyList<IXmlNode>, IEnumerable<IXmlNode>
	{
		private readonly SystemXmlNodeList _backingList;
		private readonly XmlDocument _owner;

		internal XmlNodeList(XmlDocument owner, SystemXmlNodeList backingList)
		{
			_backingList = backingList;
			_owner = owner;
		}

		public uint Length => (uint)_backingList.Count;

		public uint Size => (uint)_backingList.Count;

		public IXmlNode this[int index]
		{
			get => _owner.Wrap(_backingList[index]);
			set => _backingList[index] = _owner.Unwrap(value);
		}

		public IEnumerator<IXmlNode> GetEnumerator() => _backingList.GetEnumerator();

		global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

		public int Count
		{
			get => _backingList.Count;
			set => _backingList.Count;
		}

	}
}
