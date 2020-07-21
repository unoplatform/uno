#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.Syndication
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ISyndicationNode 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IList<global::Windows.Web.Syndication.SyndicationAttribute> AttributeExtensions
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Uri BaseUri
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IList<global::Windows.Web.Syndication.ISyndicationNode> ElementExtensions
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Language
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string NodeName
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string NodeNamespace
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string NodeValue
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Web.Syndication.ISyndicationNode.NodeName.get
		// Forced skipping of method Windows.Web.Syndication.ISyndicationNode.NodeName.set
		// Forced skipping of method Windows.Web.Syndication.ISyndicationNode.NodeNamespace.get
		// Forced skipping of method Windows.Web.Syndication.ISyndicationNode.NodeNamespace.set
		// Forced skipping of method Windows.Web.Syndication.ISyndicationNode.NodeValue.get
		// Forced skipping of method Windows.Web.Syndication.ISyndicationNode.NodeValue.set
		// Forced skipping of method Windows.Web.Syndication.ISyndicationNode.Language.get
		// Forced skipping of method Windows.Web.Syndication.ISyndicationNode.Language.set
		// Forced skipping of method Windows.Web.Syndication.ISyndicationNode.BaseUri.get
		// Forced skipping of method Windows.Web.Syndication.ISyndicationNode.BaseUri.set
		// Forced skipping of method Windows.Web.Syndication.ISyndicationNode.AttributeExtensions.get
		// Forced skipping of method Windows.Web.Syndication.ISyndicationNode.ElementExtensions.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Data.Xml.Dom.XmlDocument GetXmlDocument( global::Windows.Web.Syndication.SyndicationFormat format);
		#endif
	}
}
