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
		public const string DefaultUri = "http://localhost:55838/";
		public readonly static string iOSAppName;
		public readonly static string AndroidAppName = "uno.platform.unosampleapp";

		// Default active platform when running under Visual Studio test runner
		public const Platform CurrentPlatform = Platform.Browser;
	}
}
