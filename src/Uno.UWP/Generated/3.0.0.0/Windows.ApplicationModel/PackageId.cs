#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PackageId 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.System.ProcessorArchitecture Architecture
		{
			get
			{
				throw new global::System.NotImplementedException("The member ProcessorArchitecture PackageId.Architecture is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  string FamilyName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PackageId.FamilyName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  string FullName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PackageId.FullName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  string Name
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PackageId.Name is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  string Publisher
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PackageId.Publisher is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  string PublisherId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PackageId.PublisherId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  string ResourceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PackageId.ResourceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.ApplicationModel.PackageVersion Version
		{
			get
			{
				throw new global::System.NotImplementedException("The member PackageVersion PackageId.Version is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  string Author
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PackageId.Author is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  string ProductId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PackageId.ProductId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.PackageId.Name.get
		// Forced skipping of method Windows.ApplicationModel.PackageId.Version.get
		// Forced skipping of method Windows.ApplicationModel.PackageId.Architecture.get
		// Forced skipping of method Windows.ApplicationModel.PackageId.ResourceId.get
		// Forced skipping of method Windows.ApplicationModel.PackageId.Publisher.get
		// Forced skipping of method Windows.ApplicationModel.PackageId.PublisherId.get
		// Forced skipping of method Windows.ApplicationModel.PackageId.FullName.get
		// Forced skipping of method Windows.ApplicationModel.PackageId.FamilyName.get
		// Forced skipping of method Windows.ApplicationModel.PackageId.ProductId.get
		// Forced skipping of method Windows.ApplicationModel.PackageId.Author.get
	}
}
