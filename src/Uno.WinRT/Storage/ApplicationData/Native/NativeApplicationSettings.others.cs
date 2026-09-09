#if IS_UNIT_TESTS || __NETSTD_REFERENCE__
#nullable enable

using System.Collections.Generic;

namespace Uno.Storage;

/// <summary>
/// Settings are kept in memory only: this build flavor has no native store to persist them to. It lets the
/// container, key path and serialization logic run — and be tested — without a device.
/// </summary>
partial class NativeApplicationSettings
{
	private static partial bool SupportsLocalityPlatform() => true;

	private partial Dictionary<string, string> LoadPlatform() => new();

	private partial void SetSettingPlatform(string key, string value) { }

	private partial void RemoveSettingsPlatform(IReadOnlyCollection<string> keys) { }
}
#endif
