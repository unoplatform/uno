#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ApplicationDataContainer 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IReadOnlyDictionary<string, global::Windows.Storage.ApplicationDataContainer> Containers
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<string, ApplicationDataContainer> ApplicationDataContainer.Containers is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.ApplicationDataLocality Locality
		{
			get
			{
				throw new global::System.NotImplementedException("The member ApplicationDataLocality ApplicationDataContainer.Locality is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  string Name
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ApplicationDataContainer.Name is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Collections.IPropertySet Values
		{
			get
			{
				throw new global::System.NotImplementedException("The member IPropertySet ApplicationDataContainer.Values is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Storage.ApplicationDataContainer.Name.get
		// Forced skipping of method Windows.Storage.ApplicationDataContainer.Locality.get
		// Forced skipping of method Windows.Storage.ApplicationDataContainer.Values.get
		// Forced skipping of method Windows.Storage.ApplicationDataContainer.Containers.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.ApplicationDataContainer CreateContainer( string name,  global::Windows.Storage.ApplicationDataCreateDisposition disposition)
		{
			throw new global::System.NotImplementedException("The member ApplicationDataContainer ApplicationDataContainer.CreateContainer(string name, ApplicationDataCreateDisposition disposition) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void DeleteContainer( string name)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.ApplicationDataContainer", "void ApplicationDataContainer.DeleteContainer(string name)");
		}
		#endif
	}
}
