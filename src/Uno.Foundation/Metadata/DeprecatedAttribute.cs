#nullable disable

#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Metadata
{
	public  partial class DeprecatedAttribute : global::System.Attribute
	{
		public DeprecatedAttribute( string message,  global::Windows.Foundation.Metadata.DeprecationType type,  uint version) : base()
		{
		}

		public DeprecatedAttribute( string message,  global::Windows.Foundation.Metadata.DeprecationType type,  uint version,  global::Windows.Foundation.Metadata.Platform platform) : base()
		{
		}

		public DeprecatedAttribute( string message,  global::Windows.Foundation.Metadata.DeprecationType type,  uint version,  string contract) : base()
		{
		}
	}
}
