#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.RemoteSystems
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RemoteSystemSessionWatcher 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.System.RemoteSystems.RemoteSystemSessionWatcherStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member RemoteSystemSessionWatcherStatus RemoteSystemSessionWatcher.Status is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Start()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemSessionWatcher", "void RemoteSystemSessionWatcher.Start()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemSessionWatcher", "void RemoteSystemSessionWatcher.Stop()");
		}
		#endif
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionWatcher.Status.get
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionWatcher.Added.add
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionWatcher.Added.remove
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionWatcher.Updated.add
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionWatcher.Updated.remove
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionWatcher.Removed.add
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionWatcher.Removed.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.System.RemoteSystems.RemoteSystemSessionWatcher, global::Windows.System.RemoteSystems.RemoteSystemSessionAddedEventArgs> Added
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemSessionWatcher", "event TypedEventHandler<RemoteSystemSessionWatcher, RemoteSystemSessionAddedEventArgs> RemoteSystemSessionWatcher.Added");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemSessionWatcher", "event TypedEventHandler<RemoteSystemSessionWatcher, RemoteSystemSessionAddedEventArgs> RemoteSystemSessionWatcher.Added");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.System.RemoteSystems.RemoteSystemSessionWatcher, global::Windows.System.RemoteSystems.RemoteSystemSessionRemovedEventArgs> Removed
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemSessionWatcher", "event TypedEventHandler<RemoteSystemSessionWatcher, RemoteSystemSessionRemovedEventArgs> RemoteSystemSessionWatcher.Removed");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemSessionWatcher", "event TypedEventHandler<RemoteSystemSessionWatcher, RemoteSystemSessionRemovedEventArgs> RemoteSystemSessionWatcher.Removed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.System.RemoteSystems.RemoteSystemSessionWatcher, global::Windows.System.RemoteSystems.RemoteSystemSessionUpdatedEventArgs> Updated
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemSessionWatcher", "event TypedEventHandler<RemoteSystemSessionWatcher, RemoteSystemSessionUpdatedEventArgs> RemoteSystemSessionWatcher.Updated");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemSessionWatcher", "event TypedEventHandler<RemoteSystemSessionWatcher, RemoteSystemSessionUpdatedEventArgs> RemoteSystemSessionWatcher.Updated");
			}
		}
		#endif
	}
}
