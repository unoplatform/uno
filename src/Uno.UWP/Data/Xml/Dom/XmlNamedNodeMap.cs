using System;
using System.Collections;
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

		public IXmlNode? GetNamedItem(string name) => (IXmlNode?)_owner.Wrap(_backingNamedNodeMap.GetNamedItem(name));

		public IXmlNode? SetNamedItem(IXmlNode node) =>
			(IXmlNode?)_owner.Wrap(
				_backingNamedNodeMap.SetNamedItem(
					(SystemXmlNode)_owner.Unwrap(node)));

		public IXmlNode? RemoveNamedItem(string name) => (IXmlNode?)_owner.Wrap(_backingNamedNodeMap.RemoveNamedItem(name));

		public IXmlNode this[int index]
		{
			get => (IXmlNode)_owner.Wrap(_backingNamedNodeMap.Item(index)!);
			set => throw new InvalidOperationException("XML named node map is read-only.");
		}
		public IEnumerator<IXmlNode> GetEnumerator() => new SystemXmlNamedNodeMapEnumerator(_owner, _backingNamedNodeMap.GetEnumerator());

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		public int Count
		{
			get => _backingNamedNodeMap.Count;
			set => throw new InvalidOperationException("XML named node map is read-only.");
		}

		private class SystemXmlNamedNodeMapEnumerator : IEnumerator<IXmlNode>
		{
			private readonly IEnumerator _systemXmlEnumerator;
			private readonly XmlDocument _owner;

			public SystemXmlNamedNodeMapEnumerator(XmlDocument owner, IEnumerator systemXmlEnumerator)
			{
				_systemXmlEnumerator = systemXmlEnumerator;
				_owner = owner;
			}

			public IXmlNode Current
			{

				get
				{
					var item = _systemXmlEnumerator.Current;
					return (IXmlNode)_owner.Wrap(item);
				}
			}

			object IEnumerator.Current => this.Current;

			public bool MoveNext() => _systemXmlEnumerator.MoveNext();

			public void Reset() => _systemXmlEnumerator.Reset();

			public void Dispose()
			{
				if (_systemXmlEnumerator is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}
		}
	}
}
