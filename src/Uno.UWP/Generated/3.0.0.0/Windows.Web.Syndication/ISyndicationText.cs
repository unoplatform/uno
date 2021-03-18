#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.Syndication
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ISyndicationText : global::Windows.Web.Syndication.ISyndicationNode
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Text
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Type
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Data.Xml.Dom.XmlDocument Xml
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Web.Syndication.ISyndicationText.Text.get
		// Forced skipping of method Windows.Web.Syndication.ISyndicationText.Text.set
		// Forced skipping of method Windows.Web.Syndication.ISyndicationText.Type.get
		// Forced skipping of method Windows.Web.Syndication.ISyndicationText.Type.set
		// Forced skipping of method Windows.Web.Syndication.ISyndicationText.Xml.get
		// Forced skipping of method Windows.Web.Syndication.ISyndicationText.Xml.set
	}
}
