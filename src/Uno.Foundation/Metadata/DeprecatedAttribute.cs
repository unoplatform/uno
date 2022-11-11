using System;

namespace Windows.Foundation.Metadata
{
	public partial class DeprecatedAttribute : Attribute
	{
		public DeprecatedAttribute(string message, DeprecationType type, uint version) : base()
		{
		}

		public DeprecatedAttribute(string message, DeprecationType type, uint version, Platform platform) : base()
		{
		}

		public DeprecatedAttribute(string message, DeprecationType type, uint version, string contract) : base()
		{
		}
	}
}
