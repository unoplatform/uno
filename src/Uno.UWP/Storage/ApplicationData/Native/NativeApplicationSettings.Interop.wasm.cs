#nullable enable

using System.Runtime.InteropServices.JavaScript;
using Uno.Extensions;
using Windows.Storage;

namespace __Uno.Storage;

partial class NativeApplicationSettings
{
	internal static partial class NativeMethods
	{
		internal static bool TryGetValue(ApplicationDataLocality locality, string key, out string? value)
		{
			if (!ContainsKey(locality, key))
			{
				value = null;
				return false;
			}
			else
			{
				value = GetValue(locality.ToStringInvariant(), key);
				return true;
			}
		}

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.getValue")]
		private static partial string GetValue(string locality, string key);

		internal static void SetValue(ApplicationDataLocality locality, string key, string value)
			=> SetValue(locality.ToStringInvariant(), key, value);

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.setValue")]
		private static partial void SetValue(string locality, string key, string value);

		internal static bool ContainsKey(ApplicationDataLocality locality, string key)
			=> ContainsKey(locality.ToStringInvariant(), key);

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.containsKey")]
		private static partial bool ContainsKey(string locality, string key);

		internal static string GetKeyByIndex(ApplicationDataLocality locality, int index)
			=> GetKeyByIndex(locality.ToStringInvariant(), index);

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.getKeyByIndex")]
		private static partial string GetKeyByIndex(string locality, int index);

		internal static int GetCount(ApplicationDataLocality locality)
			=> GetCount(locality.ToStringInvariant());

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.getCount")]
		private static partial int GetCount(string locality);

		internal static void Clear(ApplicationDataLocality locality)
			=> Clear(locality.ToStringInvariant());

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.clear")]
		private static partial void Clear(string locality);

		internal static bool Remove(ApplicationDataLocality locality, string key)
			=> Remove(locality.ToStringInvariant(), key);

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.remove")]
		private static partial bool Remove(string locality, string key);

		internal static string GetValueByIndex(ApplicationDataLocality locality, int index)
			=> GetValueByIndex(locality.ToStringInvariant(), index);

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.getValueByIndex")]
		private static partial string GetValueByIndex(string locality, int index);
	}
}
