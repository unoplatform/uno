#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EventTrigger : global::Windows.UI.Xaml.TriggerBase
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.RoutedEvent RoutedEvent
		{
			get
			{
				throw new global::System.NotImplementedException("The member RoutedEvent EventTrigger.RoutedEvent is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.EventTrigger", "RoutedEvent EventTrigger.RoutedEvent");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.TriggerActionCollection Actions
		{
			get
			{
				throw new global::System.NotImplementedException("The member TriggerActionCollection EventTrigger.Actions is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public EventTrigger() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.EventTrigger", "EventTrigger.EventTrigger()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.EventTrigger.EventTrigger()
		// Forced skipping of method Windows.UI.Xaml.EventTrigger.RoutedEvent.get
		// Forced skipping of method Windows.UI.Xaml.EventTrigger.RoutedEvent.set
		// Forced skipping of method Windows.UI.Xaml.EventTrigger.Actions.get
	}
}
