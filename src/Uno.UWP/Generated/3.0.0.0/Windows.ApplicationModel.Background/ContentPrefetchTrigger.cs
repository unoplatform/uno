#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContentPrefetchTrigger : global::Windows.ApplicationModel.Background.IBackgroundTrigger
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.TimeSpan WaitInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan ContentPrefetchTrigger.WaitInterval is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public ContentPrefetchTrigger( global::System.TimeSpan waitInterval) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.ContentPrefetchTrigger", "ContentPrefetchTrigger.ContentPrefetchTrigger(TimeSpan waitInterval)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.ContentPrefetchTrigger.ContentPrefetchTrigger(System.TimeSpan)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public ContentPrefetchTrigger() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.ContentPrefetchTrigger", "ContentPrefetchTrigger.ContentPrefetchTrigger()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.ContentPrefetchTrigger.ContentPrefetchTrigger()
		// Forced skipping of method Windows.ApplicationModel.Background.ContentPrefetchTrigger.WaitInterval.get
		// Processing: Windows.ApplicationModel.Background.IBackgroundTrigger
	}
}
