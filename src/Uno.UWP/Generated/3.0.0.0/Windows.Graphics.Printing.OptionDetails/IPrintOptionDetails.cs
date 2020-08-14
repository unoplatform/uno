#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.OptionDetails
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IPrintOptionDetails 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string ErrorText
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string OptionId
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Graphics.Printing.OptionDetails.PrintOptionType OptionType
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Graphics.Printing.OptionDetails.PrintOptionStates State
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		object Value
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.OptionDetails.IPrintOptionDetails.OptionId.get
		// Forced skipping of method Windows.Graphics.Printing.OptionDetails.IPrintOptionDetails.OptionType.get
		// Forced skipping of method Windows.Graphics.Printing.OptionDetails.IPrintOptionDetails.ErrorText.set
		// Forced skipping of method Windows.Graphics.Printing.OptionDetails.IPrintOptionDetails.ErrorText.get
		// Forced skipping of method Windows.Graphics.Printing.OptionDetails.IPrintOptionDetails.State.set
		// Forced skipping of method Windows.Graphics.Printing.OptionDetails.IPrintOptionDetails.State.get
		// Forced skipping of method Windows.Graphics.Printing.OptionDetails.IPrintOptionDetails.Value.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool TrySetValue( object value);
		#endif
	}
}
