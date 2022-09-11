#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ExpressionAnimation : global::Windows.UI.Composition.CompositionAnimation
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Expression
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ExpressionAnimation.Expression is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.ExpressionAnimation", "string ExpressionAnimation.Expression");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.ExpressionAnimation.Expression.get
		// Forced skipping of method Windows.UI.Composition.ExpressionAnimation.Expression.set
	}
}
