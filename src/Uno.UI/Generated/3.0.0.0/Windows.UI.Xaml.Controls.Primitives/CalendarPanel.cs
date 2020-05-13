#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class CalendarPanel : global::Windows.UI.Xaml.Controls.Panel
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
		public CalendarPanel() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.CalendarPanel", "CalendarPanel.CalendarPanel()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.CalendarPanel.CalendarPanel()
	}
}
