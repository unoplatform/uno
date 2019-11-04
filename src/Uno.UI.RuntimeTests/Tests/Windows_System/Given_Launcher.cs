#if __ANDROID__ || __IOS__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.Core;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.StartScreen;

namespace Uno.UI.RuntimeTests.Tests.Windows_System
{
	[TestClass]
	public class Given_Launcher
	{
		private async Task Dispatch(DispatchedHandler p)
		{
			await CoreApplication.GetCurrentView().Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, p);
		}

		[TestMethod]
		public async Task When_Valid_Uri_Is_Queried()
		{
			await Dispatch(async () =>
			{
				var result = await Launcher.QueryUriSupportAsync(
				new Uri("https://platform.uno"),
				LaunchQuerySupportType.Uri);

				Assert.AreEqual(LaunchQuerySupportStatus.Available, result);
			});
		}

		[TestMethod]
		public async Task When_Unsupported_Uri_Is_Queried()
		{
			await Dispatch(async () =>
			{
				var result = await Launcher.QueryUriSupportAsync(
				new Uri("thisschemedefinitelydoesnotexist://helloworld"),
				LaunchQuerySupportType.Uri);

				Assert.AreEqual(LaunchQuerySupportStatus.NotSupported, result);
			});
		}
	}
}
#endif
