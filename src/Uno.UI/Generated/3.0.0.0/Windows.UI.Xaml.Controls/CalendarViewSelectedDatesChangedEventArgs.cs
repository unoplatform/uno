#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CalendarViewSelectedDatesChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::System.DateTimeOffset> AddedDates
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<DateTimeOffset> CalendarViewSelectedDatesChangedEventArgs.AddedDates is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::System.DateTimeOffset> RemovedDates
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<DateTimeOffset> CalendarViewSelectedDatesChangedEventArgs.RemovedDates is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.CalendarViewSelectedDatesChangedEventArgs.AddedDates.get
		// Forced skipping of method Windows.UI.Xaml.Controls.CalendarViewSelectedDatesChangedEventArgs.RemovedDates.get
	}
}
