#if IS_UNIT_TESTS || __NETSTD_REFERENCE__
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Storage;

partial class NativeApplicationSettings
{
	private partial void SetSettingPlatform(string key, string value)
	{
		throw new NotImplementedException();
	}

	private partial bool TryGetSettingPlatform(string key, out string? value)
	{
		throw new NotImplementedException();
	}

	private partial bool RemoveSettingPlatform(string key)
	{
		throw new NotImplementedException();
	}

	private static partial bool SupportsLocalityPlatform() => false;

	private partial bool ContainsSettingPlatform(string key) => throw new NotImplementedException();

	private partial IEnumerable<string> GetKeysPlatform() => throw new NotImplementedException();
}
#endif
