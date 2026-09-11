using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace __Windows.ApplicationModel.Core
{
	public partial class CoreApplicationNative
	{

		[JSImport("globalThis.Windows.ApplicationModel.Core.CoreApplication.initialize")]
		internal static partial void NativeInitialize();

		[JSImport("globalThis.Windows.ApplicationModel.Core.CoreApplication.initializeExports")]
		internal static partial Task InitializeExports();
	}
}
