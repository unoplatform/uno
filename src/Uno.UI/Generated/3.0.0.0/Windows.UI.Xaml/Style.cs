#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Style : global::Windows.UI.Xaml.DependencyObject
	{
		// Skipping already declared property TargetType
		// Skipping already declared property BasedOn
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsSealed
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool Style.IsSealed is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property Setters
		// Skipping already declared method Windows.UI.Xaml.Style.Style(System.Type)
		// Forced skipping of method Windows.UI.Xaml.Style.Style(System.Type)
		// Skipping already declared method Windows.UI.Xaml.Style.Style()
		// Forced skipping of method Windows.UI.Xaml.Style.Style()
		// Forced skipping of method Windows.UI.Xaml.Style.IsSealed.get
		// Forced skipping of method Windows.UI.Xaml.Style.Setters.get
		// Forced skipping of method Windows.UI.Xaml.Style.TargetType.get
		// Forced skipping of method Windows.UI.Xaml.Style.TargetType.set
		// Forced skipping of method Windows.UI.Xaml.Style.BasedOn.get
		// Forced skipping of method Windows.UI.Xaml.Style.BasedOn.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Seal()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Style", "void Style.Seal()");
		}
		#endif
	}
}
