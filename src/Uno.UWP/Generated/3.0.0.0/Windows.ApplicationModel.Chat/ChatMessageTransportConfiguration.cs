#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Chat
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ChatMessageTransportConfiguration 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyDictionary<string, object> ExtendedProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<string, object> ChatMessageTransportConfiguration.ExtendedProperties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int MaxAttachmentCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member int ChatMessageTransportConfiguration.MaxAttachmentCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int MaxMessageSizeInKilobytes
		{
			get
			{
				throw new global::System.NotImplementedException("The member int ChatMessageTransportConfiguration.MaxMessageSizeInKilobytes is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int MaxRecipientCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member int ChatMessageTransportConfiguration.MaxRecipientCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.MediaEncodingProfile SupportedVideoFormat
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaEncodingProfile ChatMessageTransportConfiguration.SupportedVideoFormat is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatMessageTransportConfiguration.MaxAttachmentCount.get
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatMessageTransportConfiguration.MaxMessageSizeInKilobytes.get
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatMessageTransportConfiguration.MaxRecipientCount.get
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatMessageTransportConfiguration.SupportedVideoFormat.get
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatMessageTransportConfiguration.ExtendedProperties.get
	}
}
