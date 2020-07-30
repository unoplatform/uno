#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreTextFormatUpdatingEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Text.Core.CoreTextFormatUpdatingResult Result
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreTextFormatUpdatingResult CoreTextFormatUpdatingEventArgs.Result is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Text.Core.CoreTextFormatUpdatingEventArgs", "CoreTextFormatUpdatingResult CoreTextFormatUpdatingEventArgs.Result");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.ViewManagement.UIElementType? BackgroundColor
		{
			get
			{
				throw new global::System.NotImplementedException("The member UIElementType? CoreTextFormatUpdatingEventArgs.BackgroundColor is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsCanceled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreTextFormatUpdatingEventArgs.IsCanceled is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Text.Core.CoreTextRange Range
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreTextRange CoreTextFormatUpdatingEventArgs.Range is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Text.Core.CoreTextFormatUpdatingReason Reason
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreTextFormatUpdatingReason CoreTextFormatUpdatingEventArgs.Reason is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.ViewManagement.UIElementType? TextColor
		{
			get
			{
				throw new global::System.NotImplementedException("The member UIElementType? CoreTextFormatUpdatingEventArgs.TextColor is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.ViewManagement.UIElementType? UnderlineColor
		{
			get
			{
				throw new global::System.NotImplementedException("The member UIElementType? CoreTextFormatUpdatingEventArgs.UnderlineColor is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Text.UnderlineType? UnderlineType
		{
			get
			{
				throw new global::System.NotImplementedException("The member UnderlineType? CoreTextFormatUpdatingEventArgs.UnderlineType is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Text.Core.CoreTextFormatUpdatingEventArgs.Range.get
		// Forced skipping of method Windows.UI.Text.Core.CoreTextFormatUpdatingEventArgs.TextColor.get
		// Forced skipping of method Windows.UI.Text.Core.CoreTextFormatUpdatingEventArgs.BackgroundColor.get
		// Forced skipping of method Windows.UI.Text.Core.CoreTextFormatUpdatingEventArgs.UnderlineColor.get
		// Forced skipping of method Windows.UI.Text.Core.CoreTextFormatUpdatingEventArgs.UnderlineType.get
		// Forced skipping of method Windows.UI.Text.Core.CoreTextFormatUpdatingEventArgs.Reason.get
		// Forced skipping of method Windows.UI.Text.Core.CoreTextFormatUpdatingEventArgs.Result.get
		// Forced skipping of method Windows.UI.Text.Core.CoreTextFormatUpdatingEventArgs.Result.set
		// Forced skipping of method Windows.UI.Text.Core.CoreTextFormatUpdatingEventArgs.IsCanceled.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral CoreTextFormatUpdatingEventArgs.GetDeferral() is not implemented in Uno.");
		}
		#endif
	}
}
