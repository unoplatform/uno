#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CalendarViewDayItemChangingEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool InRecycleQueue
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CalendarViewDayItemChangingEventArgs.InRecycleQueue is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.CalendarViewDayItem Item
		{
			get
			{
				throw new global::System.NotImplementedException("The member CalendarViewDayItem CalendarViewDayItemChangingEventArgs.Item is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Phase
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint CalendarViewDayItemChangingEventArgs.Phase is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.CalendarViewDayItemChangingEventArgs.InRecycleQueue.get
		// Forced skipping of method Windows.UI.Xaml.Controls.CalendarViewDayItemChangingEventArgs.Item.get
		// Forced skipping of method Windows.UI.Xaml.Controls.CalendarViewDayItemChangingEventArgs.Phase.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RegisterUpdateCallback( global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.CalendarView, global::Windows.UI.Xaml.Controls.CalendarViewDayItemChangingEventArgs> callback)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.CalendarViewDayItemChangingEventArgs", "void CalendarViewDayItemChangingEventArgs.RegisterUpdateCallback(TypedEventHandler<CalendarView, CalendarViewDayItemChangingEventArgs> callback)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RegisterUpdateCallback( uint callbackPhase,  global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.CalendarView, global::Windows.UI.Xaml.Controls.CalendarViewDayItemChangingEventArgs> callback)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.CalendarViewDayItemChangingEventArgs", "void CalendarViewDayItemChangingEventArgs.RegisterUpdateCallback(uint callbackPhase, TypedEventHandler<CalendarView, CalendarViewDayItemChangingEventArgs> callback)");
		}
		#endif
	}
}
