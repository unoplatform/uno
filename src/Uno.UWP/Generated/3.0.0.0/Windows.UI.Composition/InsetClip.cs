#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InsetClip : global::Windows.UI.Composition.CompositionClip
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  float TopInset
		{
			get
			{
				throw new global::System.NotImplementedException("The member float InsetClip.TopInset is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.InsetClip", "float InsetClip.TopInset");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  float RightInset
		{
			get
			{
				throw new global::System.NotImplementedException("The member float InsetClip.RightInset is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.InsetClip", "float InsetClip.RightInset");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  float LeftInset
		{
			get
			{
				throw new global::System.NotImplementedException("The member float InsetClip.LeftInset is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.InsetClip", "float InsetClip.LeftInset");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  float BottomInset
		{
			get
			{
				throw new global::System.NotImplementedException("The member float InsetClip.BottomInset is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.InsetClip", "float InsetClip.BottomInset");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.InsetClip.BottomInset.get
		// Forced skipping of method Windows.UI.Composition.InsetClip.BottomInset.set
		// Forced skipping of method Windows.UI.Composition.InsetClip.LeftInset.get
		// Forced skipping of method Windows.UI.Composition.InsetClip.LeftInset.set
		// Forced skipping of method Windows.UI.Composition.InsetClip.RightInset.get
		// Forced skipping of method Windows.UI.Composition.InsetClip.RightInset.set
		// Forced skipping of method Windows.UI.Composition.InsetClip.TopInset.get
		// Forced skipping of method Windows.UI.Composition.InsetClip.TopInset.set
	}
}
