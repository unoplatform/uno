#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TeachingTipClosedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.UI.Xaml.Controls.TeachingTipCloseReason Reason
		{
			get
			{
				throw new global::System.NotImplementedException("The member TeachingTipCloseReason TeachingTipClosedEventArgs.Reason is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=TeachingTipCloseReason%20TeachingTipClosedEventArgs.Reason");
			}
		}
		#endif
		// Forced skipping of method Microsoft.UI.Xaml.Controls.TeachingTipClosedEventArgs.Reason.get
	}
}
