#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Management.Policies
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NamedPolicyData 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Area
		{
			get
			{
				throw new global::System.NotImplementedException("The member string NamedPolicyData.Area is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsManaged
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool NamedPolicyData.IsManaged is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsUserPolicy
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool NamedPolicyData.IsUserPolicy is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Management.Policies.NamedPolicyKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member NamedPolicyKind NamedPolicyData.Kind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Name
		{
			get
			{
				throw new global::System.NotImplementedException("The member string NamedPolicyData.Name is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User NamedPolicyData.User is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Management.Policies.NamedPolicyData.Area.get
		// Forced skipping of method Windows.Management.Policies.NamedPolicyData.Name.get
		// Forced skipping of method Windows.Management.Policies.NamedPolicyData.Kind.get
		// Forced skipping of method Windows.Management.Policies.NamedPolicyData.IsManaged.get
		// Forced skipping of method Windows.Management.Policies.NamedPolicyData.IsUserPolicy.get
		// Forced skipping of method Windows.Management.Policies.NamedPolicyData.User.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool GetBoolean()
		{
			throw new global::System.NotImplementedException("The member bool NamedPolicyData.GetBoolean() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.Streams.IBuffer GetBinary()
		{
			throw new global::System.NotImplementedException("The member IBuffer NamedPolicyData.GetBinary() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  int GetInt32()
		{
			throw new global::System.NotImplementedException("The member int NamedPolicyData.GetInt32() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  long GetInt64()
		{
			throw new global::System.NotImplementedException("The member long NamedPolicyData.GetInt64() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string GetString()
		{
			throw new global::System.NotImplementedException("The member string NamedPolicyData.GetString() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Management.Policies.NamedPolicyData.Changed.add
		// Forced skipping of method Windows.Management.Policies.NamedPolicyData.Changed.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Management.Policies.NamedPolicyData, object> Changed
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Management.Policies.NamedPolicyData", "event TypedEventHandler<NamedPolicyData, object> NamedPolicyData.Changed");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Management.Policies.NamedPolicyData", "event TypedEventHandler<NamedPolicyData, object> NamedPolicyData.Changed");
			}
		}
		#endif
	}
}
