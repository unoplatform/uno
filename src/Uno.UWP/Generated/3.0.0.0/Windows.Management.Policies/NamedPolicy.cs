#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Management.Policies
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NamedPolicy 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Management.Policies.NamedPolicyData GetPolicyFromPath( string area,  string name)
		{
			throw new global::System.NotImplementedException("The member NamedPolicyData NamedPolicy.GetPolicyFromPath(string area, string name) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Management.Policies.NamedPolicyData GetPolicyFromPathForUser( global::Windows.System.User user,  string area,  string name)
		{
			throw new global::System.NotImplementedException("The member NamedPolicyData NamedPolicy.GetPolicyFromPathForUser(User user, string area, string name) is not implemented in Uno.");
		}
		#endif
	}
}
