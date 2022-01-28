using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests
{
	public partial class Constants
	{
		public const string WebAssemblyDefaultUri = "https://localhost:44375/";
		public const string iOSAppName = "uno.platform.uitestsample";
		public const string AndroidAppName = "uno.platform.unosampleapp";
		public const string iOSDeviceNameOrId = "iPad Pro (12.9-inch) (4th generation)";

		// Default active platform when running under Visual Studio test runner
		public const Platform CurrentPlatform =
#if TARGET_FRAMEWORK_OVERRIDE_ANDROID
			Platform.Android;
#elif TARGET_FRAMEWORK_OVERRIDE_IOS
			Platform.iOS;
#else
			Platform.Browser;
#endif
	}
}
