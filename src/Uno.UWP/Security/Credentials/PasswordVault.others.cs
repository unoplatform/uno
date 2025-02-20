#if !__ANDROID__ && !__IOS__ && !__MACOS__
using System;
using Uno;

namespace Windows.Security.Credentials;

[NotImplemented("IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
// This class is ** NOT ** sealed in order to allow projects for which the security limit described bellow is not
// really a concern (for instance if they are only storing an OAuth token) to inherit and provide they own
// implementation of 'IPersister'.
partial class PasswordVault
{
	[NotImplemented("IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
	public PasswordVault()
	{
#if !__WASM__
		throw new NotImplementedException();
#else
		throw new NotSupportedException(@"There is no way to properly persist secured content on WebAssembly.
At the opposite of other platforms, we cannot properly store a secret in a secured enclave which ensure that our secret
won't be accessed by any untrusted code (e.g. cross-site scripting).");
#endif
	}
}
#endif
