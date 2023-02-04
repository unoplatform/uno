#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.RemoteSystems
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RemoteSystemSessionParticipant 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.RemoteSystems.RemoteSystem RemoteSystem
		{
			get
			{
				throw new global::System.NotImplementedException("The member RemoteSystem RemoteSystemSessionParticipant.RemoteSystem is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=RemoteSystem%20RemoteSystemSessionParticipant.RemoteSystem");
			}
		}
		#endif
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionParticipant.RemoteSystem.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.HostName> GetHostNames()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<HostName> RemoteSystemSessionParticipant.GetHostNames() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CHostName%3E%20RemoteSystemSessionParticipant.GetHostNames%28%29");
		}
		#endif
	}
}
