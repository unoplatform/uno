#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Data.Xml.Dom
{
	#if false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IXmlCharacterData : global::Windows.Data.Xml.Dom.IXmlNode,global::Windows.Data.Xml.Dom.IXmlNodeSelector,global::Windows.Data.Xml.Dom.IXmlNodeSerializer
	{
		#if false
		string Data
		{
			get;
			set;
		}
		#endif
		#if false
		uint Length
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Data.Xml.Dom.IXmlCharacterData.Data.get
		// Forced skipping of method Windows.Data.Xml.Dom.IXmlCharacterData.Data.set
		// Forced skipping of method Windows.Data.Xml.Dom.IXmlCharacterData.Length.get
		#if false
		string SubstringData( uint offset,  uint count);
		#endif
		#if false
		void AppendData( string data);
		#endif
		#if false
		void InsertData( uint offset,  string data);
		#endif
		#if false
		void DeleteData( uint offset,  uint count);
		#endif
		#if false
		void ReplaceData( uint offset,  uint count,  string data);
		#endif
	}
}
