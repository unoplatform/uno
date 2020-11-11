using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests
{
	public class Constants
	{
		public const string WebAssemblyDefaultUri = "http://localhost:55838/";
		public const string iOSAppName = "uno.platform.uitestsample";
		public const string AndroidAppName = "uno.platform.unosampleapp";
		public const string iOSDeviceNameOrId = "iPad Pro (12.9-inch) (4th generation)";

		// Default active platform when running under Visual Studio test runner
		public const Platform CurrentPlatform = Platform.Android;
	}
}
