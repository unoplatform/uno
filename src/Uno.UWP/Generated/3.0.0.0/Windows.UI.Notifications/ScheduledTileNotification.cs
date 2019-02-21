#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ScheduledTileNotification 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Tag
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ScheduledTileNotification.Tag is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.ScheduledTileNotification", "string ScheduledTileNotification.Tag");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ScheduledTileNotification.Id is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.ScheduledTileNotification", "string ScheduledTileNotification.Id");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.DateTimeOffset? ExpirationTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset? ScheduledTileNotification.ExpirationTime is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.ScheduledTileNotification", "DateTimeOffset? ScheduledTileNotification.ExpirationTime");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Data.Xml.Dom.XmlDocument Content
		{
			get
			{
				throw new global::System.NotImplementedException("The member XmlDocument ScheduledTileNotification.Content is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.DateTimeOffset DeliveryTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset ScheduledTileNotification.DeliveryTime is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public ScheduledTileNotification( global::Windows.Data.Xml.Dom.XmlDocument content,  global::System.DateTimeOffset deliveryTime) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.ScheduledTileNotification", "ScheduledTileNotification.ScheduledTileNotification(XmlDocument content, DateTimeOffset deliveryTime)");
		}
		#endif
		// Forced skipping of method Windows.UI.Notifications.ScheduledTileNotification.ScheduledTileNotification(Windows.Data.Xml.Dom.XmlDocument, System.DateTimeOffset)
		// Forced skipping of method Windows.UI.Notifications.ScheduledTileNotification.Content.get
		// Forced skipping of method Windows.UI.Notifications.ScheduledTileNotification.DeliveryTime.get
		// Forced skipping of method Windows.UI.Notifications.ScheduledTileNotification.ExpirationTime.set
		// Forced skipping of method Windows.UI.Notifications.ScheduledTileNotification.ExpirationTime.get
		// Forced skipping of method Windows.UI.Notifications.ScheduledTileNotification.Tag.set
		// Forced skipping of method Windows.UI.Notifications.ScheduledTileNotification.Tag.get
		// Forced skipping of method Windows.UI.Notifications.ScheduledTileNotification.Id.set
		// Forced skipping of method Windows.UI.Notifications.ScheduledTileNotification.Id.get
	}
}
