#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Documents
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Span : global::Windows.UI.Xaml.Documents.Inline
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Documents.InlineCollection Inlines
		{
			get
			{
				throw new global::System.NotImplementedException("The member InlineCollection Span.Inlines is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Documents.Span", "InlineCollection Span.Inlines");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public Span() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Documents.Span", "Span.Span()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Documents.Span.Span()
		// Forced skipping of method Windows.UI.Xaml.Documents.Span.Inlines.get
		// Forced skipping of method Windows.UI.Xaml.Documents.Span.Inlines.set
	}
}
