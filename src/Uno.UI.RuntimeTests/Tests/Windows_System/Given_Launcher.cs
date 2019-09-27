#if __ANDROID__ || __IOS__ || NETFX_CORE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.System;
using Windows.UI.StartScreen;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_StartScreen
{
	[TestClass]
	public class Given_Launcher
	{
		[TestMethod]
		public async Task When_Valid_Uri_Is_Queried()
		{
			var result = await Launcher.QueryUriSupportAsync(
				new Uri("https://platform.uno"),
				LaunchQuerySupportType.Uri);

			Assert.AreEqual(LaunchQuerySupportStatus.Available, result);
		}

		[TestMethod]
		public async Task When_Unsupported_Uri_Is_Queried()
		{
			var result = await Launcher.QueryUriSupportAsync(
				new Uri("thisschemedefinitelydoesnotexist://helloworld"),
				LaunchQuerySupportType.Uri);

			Assert.AreEqual(LaunchQuerySupportStatus.NotSupported, result);
		}
	}
}
#endif
