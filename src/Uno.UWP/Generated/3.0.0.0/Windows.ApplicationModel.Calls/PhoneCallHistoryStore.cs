#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Calls
{
	#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PhoneCallHistoryStore 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Calls.PhoneCallHistoryEntry> GetEntryAsync( string callHistoryEntryId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PhoneCallHistoryEntry> PhoneCallHistoryStore.GetEntryAsync(string callHistoryEntryId) is not implemented in Uno.");
		}
		#endif
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Calls.PhoneCallHistoryEntryReader GetEntryReader()
		{
			throw new global::System.NotImplementedException("The member PhoneCallHistoryEntryReader PhoneCallHistoryStore.GetEntryReader() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Calls.PhoneCallHistoryEntryReader GetEntryReader( global::Windows.ApplicationModel.Calls.PhoneCallHistoryEntryQueryOptions queryOptions)
		{
			throw new global::System.NotImplementedException("The member PhoneCallHistoryEntryReader PhoneCallHistoryStore.GetEntryReader(PhoneCallHistoryEntryQueryOptions queryOptions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SaveEntryAsync( global::Windows.ApplicationModel.Calls.PhoneCallHistoryEntry callHistoryEntry)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PhoneCallHistoryStore.SaveEntryAsync(PhoneCallHistoryEntry callHistoryEntry) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction DeleteEntryAsync( global::Windows.ApplicationModel.Calls.PhoneCallHistoryEntry callHistoryEntry)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PhoneCallHistoryStore.DeleteEntryAsync(PhoneCallHistoryEntry callHistoryEntry) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction DeleteEntriesAsync( global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Calls.PhoneCallHistoryEntry> callHistoryEntries)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PhoneCallHistoryStore.DeleteEntriesAsync(IEnumerable<PhoneCallHistoryEntry> callHistoryEntries) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction MarkEntryAsSeenAsync( global::Windows.ApplicationModel.Calls.PhoneCallHistoryEntry callHistoryEntry)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PhoneCallHistoryStore.MarkEntryAsSeenAsync(PhoneCallHistoryEntry callHistoryEntry) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction MarkEntriesAsSeenAsync( global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Calls.PhoneCallHistoryEntry> callHistoryEntries)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PhoneCallHistoryStore.MarkEntriesAsSeenAsync(IEnumerable<PhoneCallHistoryEntry> callHistoryEntries) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<uint> GetUnseenCountAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<uint> PhoneCallHistoryStore.GetUnseenCountAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction MarkAllAsSeenAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PhoneCallHistoryStore.MarkAllAsSeenAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<uint> GetSourcesUnseenCountAsync( global::System.Collections.Generic.IEnumerable<string> sourceIds)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<uint> PhoneCallHistoryStore.GetSourcesUnseenCountAsync(IEnumerable<string> sourceIds) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction MarkSourcesAsSeenAsync( global::System.Collections.Generic.IEnumerable<string> sourceIds)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PhoneCallHistoryStore.MarkSourcesAsSeenAsync(IEnumerable<string> sourceIds) is not implemented in Uno.");
		}
		#endif
	}
}
