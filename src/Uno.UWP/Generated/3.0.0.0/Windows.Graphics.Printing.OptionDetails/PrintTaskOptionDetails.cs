#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.OptionDetails
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintTaskOptionDetails : global::Windows.Graphics.Printing.IPrintTaskOptionsCore,global::Windows.Graphics.Printing.IPrintTaskOptionsCoreUIConfiguration
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<string> DisplayedOptions
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<string> PrintTaskOptionDetails.DisplayedOptions is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyDictionary<string, global::Windows.Graphics.Printing.OptionDetails.IPrintOptionDetails> Options
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<string, IPrintOptionDetails> PrintTaskOptionDetails.Options is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.OptionDetails.PrintTaskOptionDetails.Options.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.OptionDetails.PrintCustomItemListOptionDetails CreateItemListOption( string optionId,  string displayName)
		{
			throw new global::System.NotImplementedException("The member PrintCustomItemListOptionDetails PrintTaskOptionDetails.CreateItemListOption(string optionId, string displayName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.OptionDetails.PrintCustomTextOptionDetails CreateTextOption( string optionId,  string displayName)
		{
			throw new global::System.NotImplementedException("The member PrintCustomTextOptionDetails PrintTaskOptionDetails.CreateTextOption(string optionId, string displayName) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.OptionDetails.PrintTaskOptionDetails.OptionChanged.add
		// Forced skipping of method Windows.Graphics.Printing.OptionDetails.PrintTaskOptionDetails.OptionChanged.remove
		// Forced skipping of method Windows.Graphics.Printing.OptionDetails.PrintTaskOptionDetails.BeginValidation.add
		// Forced skipping of method Windows.Graphics.Printing.OptionDetails.PrintTaskOptionDetails.BeginValidation.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.PrintPageDescription GetPageDescription( uint jobPageNumber)
		{
			throw new global::System.NotImplementedException("The member PrintPageDescription PrintTaskOptionDetails.GetPageDescription(uint jobPageNumber) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.OptionDetails.PrintTaskOptionDetails.DisplayedOptions.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.OptionDetails.PrintCustomToggleOptionDetails CreateToggleOption( string optionId,  string displayName)
		{
			throw new global::System.NotImplementedException("The member PrintCustomToggleOptionDetails PrintTaskOptionDetails.CreateToggleOption(string optionId, string displayName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Graphics.Printing.OptionDetails.PrintTaskOptionDetails GetFromPrintTaskOptions( global::Windows.Graphics.Printing.PrintTaskOptions printTaskOptions)
		{
			throw new global::System.NotImplementedException("The member PrintTaskOptionDetails PrintTaskOptionDetails.GetFromPrintTaskOptions(PrintTaskOptions printTaskOptions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Printing.OptionDetails.PrintTaskOptionDetails, object> BeginValidation
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.OptionDetails.PrintTaskOptionDetails", "event TypedEventHandler<PrintTaskOptionDetails, object> PrintTaskOptionDetails.BeginValidation");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.OptionDetails.PrintTaskOptionDetails", "event TypedEventHandler<PrintTaskOptionDetails, object> PrintTaskOptionDetails.BeginValidation");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Printing.OptionDetails.PrintTaskOptionDetails, global::Windows.Graphics.Printing.OptionDetails.PrintTaskOptionChangedEventArgs> OptionChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.OptionDetails.PrintTaskOptionDetails", "event TypedEventHandler<PrintTaskOptionDetails, PrintTaskOptionChangedEventArgs> PrintTaskOptionDetails.OptionChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.OptionDetails.PrintTaskOptionDetails", "event TypedEventHandler<PrintTaskOptionDetails, PrintTaskOptionChangedEventArgs> PrintTaskOptionDetails.OptionChanged");
			}
		}
		#endif
		// Processing: Windows.Graphics.Printing.IPrintTaskOptionsCore
		// Processing: Windows.Graphics.Printing.IPrintTaskOptionsCoreUIConfiguration
	}
}
