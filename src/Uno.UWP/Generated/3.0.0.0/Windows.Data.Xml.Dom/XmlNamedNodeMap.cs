#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Data.Xml.Dom
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class XmlNamedNodeMap : global::System.Collections.Generic.IReadOnlyList<global::Windows.Data.Xml.Dom.IXmlNode>,global::System.Collections.Generic.IEnumerable<global::Windows.Data.Xml.Dom.IXmlNode>
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint Length
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint XmlNamedNodeMap.Length is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint Size
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint XmlNamedNodeMap.Size is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Data.Xml.Dom.XmlNamedNodeMap.Length.get
		// Forced skipping of method Windows.Data.Xml.Dom.XmlNamedNodeMap.Item(uint)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Data.Xml.Dom.IXmlNode GetNamedItem( string name)
		{
			throw new global::System.NotImplementedException("The member IXmlNode XmlNamedNodeMap.GetNamedItem(string name) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Data.Xml.Dom.IXmlNode SetNamedItem( global::Windows.Data.Xml.Dom.IXmlNode node)
		{
			throw new global::System.NotImplementedException("The member IXmlNode XmlNamedNodeMap.SetNamedItem(IXmlNode node) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Data.Xml.Dom.IXmlNode RemoveNamedItem( string name)
		{
			throw new global::System.NotImplementedException("The member IXmlNode XmlNamedNodeMap.RemoveNamedItem(string name) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Data.Xml.Dom.IXmlNode GetNamedItemNS( object namespaceUri,  string name)
		{
			throw new global::System.NotImplementedException("The member IXmlNode XmlNamedNodeMap.GetNamedItemNS(object namespaceUri, string name) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Data.Xml.Dom.IXmlNode RemoveNamedItemNS( object namespaceUri,  string name)
		{
			throw new global::System.NotImplementedException("The member IXmlNode XmlNamedNodeMap.RemoveNamedItemNS(object namespaceUri, string name) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Data.Xml.Dom.IXmlNode SetNamedItemNS( global::Windows.Data.Xml.Dom.IXmlNode node)
		{
			throw new global::System.NotImplementedException("The member IXmlNode XmlNamedNodeMap.SetNamedItemNS(IXmlNode node) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Data.Xml.Dom.XmlNamedNodeMap.GetAt(uint)
		// Forced skipping of method Windows.Data.Xml.Dom.XmlNamedNodeMap.Size.get
		// Forced skipping of method Windows.Data.Xml.Dom.XmlNamedNodeMap.IndexOf(Windows.Data.Xml.Dom.IXmlNode, out uint)
		// Forced skipping of method Windows.Data.Xml.Dom.XmlNamedNodeMap.GetMany(uint, Windows.Data.Xml.Dom.IXmlNode[])
		// Forced skipping of method Windows.Data.Xml.Dom.XmlNamedNodeMap.First()
		// Processing: System.Collections.Generic.IReadOnlyList<Windows.Data.Xml.Dom.IXmlNode>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public global::Windows.Data.Xml.Dom.IXmlNode this[int index]
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
		#endif
		// Processing: System.Collections.Generic.IEnumerable<Windows.Data.Xml.Dom.IXmlNode>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IEnumerable<Windows.Data.Xml.Dom.IXmlNode>
		[global::Uno.NotImplemented]
		public global::System.Collections.Generic.IEnumerator<global::Windows.Data.Xml.Dom.IXmlNode> GetEnumerator()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		// Processing: System.Collections.IEnumerable
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		// DeclaringType: System.Collections.IEnumerable
		[global::Uno.NotImplemented]
		 global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		// Processing: System.Collections.Generic.IReadOnlyCollection<Windows.Data.Xml.Dom.IXmlNode>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
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
		#endif
	}
}
