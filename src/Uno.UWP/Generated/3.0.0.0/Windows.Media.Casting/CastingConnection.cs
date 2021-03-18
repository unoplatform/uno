#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Casting
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CastingConnection : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Casting.CastingSource Source
		{
			get
			{
				throw new global::System.NotImplementedException("The member CastingSource CastingConnection.Source is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Casting.CastingConnection", "CastingSource CastingConnection.Source");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Casting.CastingDevice Device
		{
			get
			{
				throw new global::System.NotImplementedException("The member CastingDevice CastingConnection.Device is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Casting.CastingConnectionState State
		{
			get
			{
				throw new global::System.NotImplementedException("The member CastingConnectionState CastingConnection.State is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Casting.CastingConnection.State.get
		// Forced skipping of method Windows.Media.Casting.CastingConnection.Device.get
		// Forced skipping of method Windows.Media.Casting.CastingConnection.Source.get
		// Forced skipping of method Windows.Media.Casting.CastingConnection.Source.set
		// Forced skipping of method Windows.Media.Casting.CastingConnection.StateChanged.add
		// Forced skipping of method Windows.Media.Casting.CastingConnection.StateChanged.remove
		// Forced skipping of method Windows.Media.Casting.CastingConnection.ErrorOccurred.add
		// Forced skipping of method Windows.Media.Casting.CastingConnection.ErrorOccurred.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Casting.CastingConnectionErrorStatus> RequestStartCastingAsync( global::Windows.Media.Casting.CastingSource value)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CastingConnectionErrorStatus> CastingConnection.RequestStartCastingAsync(CastingSource value) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Casting.CastingConnectionErrorStatus> DisconnectAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CastingConnectionErrorStatus> CastingConnection.DisconnectAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Casting.CastingConnection", "void CastingConnection.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Casting.CastingConnection, global::Windows.Media.Casting.CastingConnectionErrorOccurredEventArgs> ErrorOccurred
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Casting.CastingConnection", "event TypedEventHandler<CastingConnection, CastingConnectionErrorOccurredEventArgs> CastingConnection.ErrorOccurred");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Casting.CastingConnection", "event TypedEventHandler<CastingConnection, CastingConnectionErrorOccurredEventArgs> CastingConnection.ErrorOccurred");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Casting.CastingConnection, object> StateChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Casting.CastingConnection", "event TypedEventHandler<CastingConnection, object> CastingConnection.StateChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Casting.CastingConnection", "event TypedEventHandler<CastingConnection, object> CastingConnection.StateChanged");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
