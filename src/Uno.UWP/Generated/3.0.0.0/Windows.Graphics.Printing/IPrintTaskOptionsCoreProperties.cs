#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IPrintTaskOptionsCoreProperties 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Graphics.Printing.PrintBinding Binding
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Graphics.Printing.PrintCollation Collation
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Graphics.Printing.PrintColorMode ColorMode
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Graphics.Printing.PrintDuplex Duplex
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Graphics.Printing.PrintHolePunch HolePunch
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		uint MaxCopies
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Graphics.Printing.PrintMediaSize MediaSize
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Graphics.Printing.PrintMediaType MediaType
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		uint MinCopies
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		uint NumberOfCopies
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Graphics.Printing.PrintOrientation Orientation
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Graphics.Printing.PrintQuality PrintQuality
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Graphics.Printing.PrintStaple Staple
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.MediaSize.set
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.MediaSize.get
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.MediaType.set
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.MediaType.get
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.Orientation.set
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.Orientation.get
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.PrintQuality.set
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.PrintQuality.get
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.ColorMode.set
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.ColorMode.get
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.Duplex.set
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.Duplex.get
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.Collation.set
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.Collation.get
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.Staple.set
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.Staple.get
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.HolePunch.set
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.HolePunch.get
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.Binding.set
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.Binding.get
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.MinCopies.get
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.MaxCopies.get
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.NumberOfCopies.set
		// Forced skipping of method Windows.Graphics.Printing.IPrintTaskOptionsCoreProperties.NumberOfCopies.get
	}
}
