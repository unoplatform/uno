#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Markup
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MarkupExtension 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public MarkupExtension() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Markup.MarkupExtension", "MarkupExtension.MarkupExtension()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Markup.MarkupExtension.MarkupExtension()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		protected virtual object ProvideValue()
		{
			throw new global::System.NotImplementedException("The member object MarkupExtension.ProvideValue() is not implemented in Uno.");
		}
		#endif
	}
}
