#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaBinder 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Token
		{
			get
			{
				throw new global::System.NotImplementedException("The member string MediaBinder.Token is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MediaBinder", "string MediaBinder.Token");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Core.MediaSource Source
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaSource MediaBinder.Source is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public MediaBinder() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MediaBinder", "MediaBinder.MediaBinder()");
		}
		#endif
		// Forced skipping of method Windows.Media.Core.MediaBinder.MediaBinder()
		// Forced skipping of method Windows.Media.Core.MediaBinder.Binding.add
		// Forced skipping of method Windows.Media.Core.MediaBinder.Binding.remove
		// Forced skipping of method Windows.Media.Core.MediaBinder.Token.get
		// Forced skipping of method Windows.Media.Core.MediaBinder.Token.set
		// Forced skipping of method Windows.Media.Core.MediaBinder.Source.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Core.MediaBinder, global::Windows.Media.Core.MediaBindingEventArgs> Binding
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MediaBinder", "event TypedEventHandler<MediaBinder, MediaBindingEventArgs> MediaBinder.Binding");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MediaBinder", "event TypedEventHandler<MediaBinder, MediaBindingEventArgs> MediaBinder.Binding");
			}
		}
		#endif
	}
}
