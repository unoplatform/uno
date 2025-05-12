#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Storage;

partial class NativeApplicationSettings
{
	private partial void SetSetting(string key, string value)
	{
		throw new NotImplementedException();
	}

	private partial bool TryGetSetting(string key, out string? value)
	{
		throw new NotImplementedException();
	}

	private partial bool RemoveSetting(string key)
	{
		throw new NotImplementedException();
	}

	private static partial bool SupportsLocality() => false;

	private partial bool ContainsSetting(string key) => throw new NotImplementedException();
}
