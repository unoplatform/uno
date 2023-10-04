#nullable enable
using System.Runtime.InteropServices.JavaScript;

namespace __Windows.Storage
{
	internal partial class ApplicationDataContainerNative
	{
		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.tryGetValue")]
		internal static partial JSObject NativeTryGetValue(string locality, string key);

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.setValue")]
		internal static partial void NativeSetValue(string locality, string key, string value);

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.containsKey")]
		internal static partial bool NativeContainsKey(string locality, string key);

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.getKeyByIndex")]
		internal static partial string NativeGetKeyByIndex(string locality, int index);

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.getCount")]
		internal static partial int NativeGetCount(string locality);

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.clear")]
		internal static partial void NativeClear(string locality);

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.remove")]
		internal static partial bool NativeRemove(string locality, string key);

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.getValueByIndex")]
		internal static partial string NativeGetValueByIndex(string locality, int index);
	}
}
