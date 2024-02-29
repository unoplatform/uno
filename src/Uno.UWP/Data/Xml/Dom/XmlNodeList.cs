using System;
using System.Collections;
using System.Collections.Generic;
using SystemXmlNodeList = System.Xml.XmlNodeList;

namespace Windows.Data.Xml.Dom
{
	public partial class XmlNodeList : IReadOnlyList<IXmlNode>, IEnumerable<IXmlNode>
	{
		private readonly XmlDocument _owner;
		internal readonly SystemXmlNodeList _backingList;

		internal XmlNodeList(XmlDocument owner, SystemXmlNodeList backingList)
		{
			_backingList = backingList;
			_owner = owner;
		}

		public uint Length => (uint)_backingList.Count;

		public uint Size => (uint)_backingList.Count;

		public IXmlNode this[int index]
		{
			get => (IXmlNode)_owner.Wrap(_backingList[index]!);
			set => throw new InvalidOperationException("List is read-only");
		}

		public IEnumerator<IXmlNode> GetEnumerator() => new SystemXmlNodeListEnumerator(_owner, _backingList.GetEnumerator());

		IEnumerator IEnumerable.GetEnumerator() =>
			this.GetEnumerator();

		public int Count
		{
			get => _backingList.Count;
			set => throw new InvalidOperationException("List is read-only");
		}

		private class SystemXmlNodeListEnumerator : IEnumerator<IXmlNode>
		{
			private readonly IEnumerator _systemXmlEnumerator;
			private readonly XmlDocument _owner;

			public SystemXmlNodeListEnumerator(XmlDocument owner, IEnumerator systemXmlEnumerator)
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
