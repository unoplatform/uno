#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.AllJoyn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AllJoynSession 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member int AllJoynSession.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member int AllJoynSession.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynSession.Id.get
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynSession.Status.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<int> RemoveMemberAsync( string uniqueName)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<int> AllJoynSession.RemoveMemberAsync(string uniqueName) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynSession.MemberAdded.add
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynSession.MemberAdded.remove
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynSession.MemberRemoved.add
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynSession.MemberRemoved.remove
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynSession.Lost.add
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynSession.Lost.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.AllJoyn.AllJoynSession> GetFromServiceInfoAsync( global::Windows.Devices.AllJoyn.AllJoynServiceInfo serviceInfo)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AllJoynSession> AllJoynSession.GetFromServiceInfoAsync(AllJoynServiceInfo serviceInfo) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.AllJoyn.AllJoynSession> GetFromServiceInfoAsync( global::Windows.Devices.AllJoyn.AllJoynServiceInfo serviceInfo,  global::Windows.Devices.AllJoyn.AllJoynBusAttachment busAttachment)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AllJoynSession> AllJoynSession.GetFromServiceInfoAsync(AllJoynServiceInfo serviceInfo, AllJoynBusAttachment busAttachment) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.AllJoyn.AllJoynSession, global::Windows.Devices.AllJoyn.AllJoynSessionLostEventArgs> Lost
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynSession", "event TypedEventHandler<AllJoynSession, AllJoynSessionLostEventArgs> AllJoynSession.Lost");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynSession", "event TypedEventHandler<AllJoynSession, AllJoynSessionLostEventArgs> AllJoynSession.Lost");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.AllJoyn.AllJoynSession, global::Windows.Devices.AllJoyn.AllJoynSessionMemberAddedEventArgs> MemberAdded
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynSession", "event TypedEventHandler<AllJoynSession, AllJoynSessionMemberAddedEventArgs> AllJoynSession.MemberAdded");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynSession", "event TypedEventHandler<AllJoynSession, AllJoynSessionMemberAddedEventArgs> AllJoynSession.MemberAdded");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.AllJoyn.AllJoynSession, global::Windows.Devices.AllJoyn.AllJoynSessionMemberRemovedEventArgs> MemberRemoved
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynSession", "event TypedEventHandler<AllJoynSession, AllJoynSessionMemberRemovedEventArgs> AllJoynSession.MemberRemoved");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynSession", "event TypedEventHandler<AllJoynSession, AllJoynSessionMemberRemovedEventArgs> AllJoynSession.MemberRemoved");
			}
		}
		#endif
	}
}
