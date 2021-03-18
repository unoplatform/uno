#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.AppService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppServiceConnection : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string PackageFamilyName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppServiceConnection.PackageFamilyName is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.AppService.AppServiceConnection", "string AppServiceConnection.PackageFamilyName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string AppServiceName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppServiceConnection.AppServiceName is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.AppService.AppServiceConnection", "string AppServiceConnection.AppServiceName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User AppServiceConnection.User is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.AppService.AppServiceConnection", "User AppServiceConnection.User");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AppServiceConnection() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.AppService.AppServiceConnection", "AppServiceConnection.AppServiceConnection()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.AppService.AppServiceConnection.AppServiceConnection()
		// Forced skipping of method Windows.ApplicationModel.AppService.AppServiceConnection.AppServiceName.get
		// Forced skipping of method Windows.ApplicationModel.AppService.AppServiceConnection.AppServiceName.set
		// Forced skipping of method Windows.ApplicationModel.AppService.AppServiceConnection.PackageFamilyName.get
		// Forced skipping of method Windows.ApplicationModel.AppService.AppServiceConnection.PackageFamilyName.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.AppService.AppServiceConnectionStatus> OpenAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AppServiceConnectionStatus> AppServiceConnection.OpenAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.AppService.AppServiceResponse> SendMessageAsync( global::Windows.Foundation.Collections.ValueSet message)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AppServiceResponse> AppServiceConnection.SendMessageAsync(ValueSet message) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.AppService.AppServiceConnection.RequestReceived.add
		// Forced skipping of method Windows.ApplicationModel.AppService.AppServiceConnection.RequestReceived.remove
		// Forced skipping of method Windows.ApplicationModel.AppService.AppServiceConnection.ServiceClosed.add
		// Forced skipping of method Windows.ApplicationModel.AppService.AppServiceConnection.ServiceClosed.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.AppService.AppServiceConnectionStatus> OpenRemoteAsync( global::Windows.System.RemoteSystems.RemoteSystemConnectionRequest remoteSystemConnectionRequest)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AppServiceConnectionStatus> AppServiceConnection.OpenRemoteAsync(RemoteSystemConnectionRequest remoteSystemConnectionRequest) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.AppService.AppServiceConnection.User.get
		// Forced skipping of method Windows.ApplicationModel.AppService.AppServiceConnection.User.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.AppService.AppServiceConnection", "void AppServiceConnection.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.AppService.StatelessAppServiceResponse> SendStatelessMessageAsync( global::Windows.ApplicationModel.AppService.AppServiceConnection connection,  global::Windows.System.RemoteSystems.RemoteSystemConnectionRequest connectionRequest,  global::Windows.Foundation.Collections.ValueSet message)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StatelessAppServiceResponse> AppServiceConnection.SendStatelessMessageAsync(AppServiceConnection connection, RemoteSystemConnectionRequest connectionRequest, ValueSet message) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.AppService.AppServiceConnection, global::Windows.ApplicationModel.AppService.AppServiceRequestReceivedEventArgs> RequestReceived
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.AppService.AppServiceConnection", "event TypedEventHandler<AppServiceConnection, AppServiceRequestReceivedEventArgs> AppServiceConnection.RequestReceived");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.AppService.AppServiceConnection", "event TypedEventHandler<AppServiceConnection, AppServiceRequestReceivedEventArgs> AppServiceConnection.RequestReceived");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.AppService.AppServiceConnection, global::Windows.ApplicationModel.AppService.AppServiceClosedEventArgs> ServiceClosed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.AppService.AppServiceConnection", "event TypedEventHandler<AppServiceConnection, AppServiceClosedEventArgs> AppServiceConnection.ServiceClosed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.AppService.AppServiceConnection", "event TypedEventHandler<AppServiceConnection, AppServiceClosedEventArgs> AppServiceConnection.ServiceClosed");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
