using System;
using Uno;

namespace Windows.Security.Credentials
{
	[NotImplemented] // Not really not implemented, but this will display an error directly in the IDE.
	/* sealed */ partial class PasswordVault
	{
		// This class is ** NOT ** sealed in order to allow projects for which the security limit described bellow is not
		// really a concern (for instance if they are only storing an OAuth token) to inherit and provide they own
		// implementation of 'IPersister'.

		private const string _notSupported = @"There is no way to properly persist secured content on WebAssembly.
At the opposite of other platforms, we cannot properly store a secret in a secured enclave which ensure that our secret
won't be accessed by any untrusted code (e.g. cross-site scripting).";

		[NotImplemented] // Not really not implemented, but this will display an error directly in the IDE.
		public PasswordVault()
		{
			throw new NotSupportedException(_notSupported);
		}
	}
}
