#nullable enable

using System.Runtime.InteropServices.JavaScript;

namespace __Windows.ApplicationModel.Core
{
	public partial class CoreApplicationNative
	{

		[JSImport("globalThis.Windows.ApplicationModel.Core.CoreApplication.initialize")]
		internal static partial void NativeInitialize();
	}
}
