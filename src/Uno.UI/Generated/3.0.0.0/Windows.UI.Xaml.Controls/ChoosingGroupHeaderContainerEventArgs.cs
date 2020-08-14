#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ChoosingGroupHeaderContainerEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.ListViewBaseHeaderItem GroupHeaderContainer
		{
			get
			{
				throw new global::System.NotImplementedException("The member ListViewBaseHeaderItem ChoosingGroupHeaderContainerEventArgs.GroupHeaderContainer is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ChoosingGroupHeaderContainerEventArgs", "ListViewBaseHeaderItem ChoosingGroupHeaderContainerEventArgs.GroupHeaderContainer");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  object Group
		{
			get
			{
				throw new global::System.NotImplementedException("The member object ChoosingGroupHeaderContainerEventArgs.Group is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int GroupIndex
		{
			get
			{
				throw new global::System.NotImplementedException("The member int ChoosingGroupHeaderContainerEventArgs.GroupIndex is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ChoosingGroupHeaderContainerEventArgs() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ChoosingGroupHeaderContainerEventArgs", "ChoosingGroupHeaderContainerEventArgs.ChoosingGroupHeaderContainerEventArgs()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ChoosingGroupHeaderContainerEventArgs.ChoosingGroupHeaderContainerEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Controls.ChoosingGroupHeaderContainerEventArgs.GroupHeaderContainer.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ChoosingGroupHeaderContainerEventArgs.GroupHeaderContainer.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ChoosingGroupHeaderContainerEventArgs.GroupIndex.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ChoosingGroupHeaderContainerEventArgs.Group.get
	}
}
