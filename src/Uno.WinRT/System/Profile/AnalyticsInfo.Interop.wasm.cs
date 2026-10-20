using System.Runtime.InteropServices.JavaScript;

namespace __Windows.__System.Profile
{
	internal partial class AnalyticsInfo
	{
		internal static partial class NativeMethods
		{
			private const string JsType = "globalThis.Windows.System.Profile.AnalyticsInfo";

			[JSImport($"{JsType}.getDeviceType")]
			internal static partial string GetDeviceType();
		}
	}
}
