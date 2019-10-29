#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel
{
	public  partial class PackageId 
	{
		[global::Uno.NotImplemented]
		public global::Windows.System.ProcessorArchitecture Architecture
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.PackageId", "Architecture");
				return System.ProcessorArchitecture.Unknown;
			}
		}

#if !__ANDROID__ && !__IOS__
		[global::Uno.NotImplemented]
		public string FamilyName
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.PackageId", "FamilyName");
				return "Unknown";
			}
		}

		[global::Uno.NotImplemented]
		public string FullName
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.PackageId", "FullName");
				return "Unknown";
			}
		}

		[global::Uno.NotImplemented]
		public string Name
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.PackageId", "Name");
				return "Unknown";
			}
		}

		[global::Uno.NotImplemented]
		public global::Windows.ApplicationModel.PackageVersion Version
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.PackageId", "Version");
				return new PackageVersion();
			}
		}
#endif

		[global::Uno.NotImplemented]
		public string Publisher
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.PackageId", "Publisher");
				return "Unknown";
			}
		}

		[global::Uno.NotImplemented]
		public string PublisherId
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.PackageId", "PublisherId");
				return "Unknown";
			}
		}

		[global::Uno.NotImplemented]
		public string ResourceId
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.PackageId", "ResourceId");
				return "Unknown";
			}
		}

		[global::Uno.NotImplemented]
		public string Author
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.PackageId", "Author");
				return "Unknown";
			}
		}

		[global::Uno.NotImplemented]
		public string ProductId
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.PackageId", "ProductId");
				return "Unknown";
			}
		}
	}
}
