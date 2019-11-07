#if __ANDROID__ || __IOS__
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.Core;
using Windows.System;
using Windows.UI.Core;

namespace Uno.UI.RuntimeTests.Tests.Windows_System
{
	[TestClass]
	public class Given_Launcher
	{
		[TestMethod]
		public async Task When_Null_Uri_Is_Launched()
		{
			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => Launcher.LaunchUriAsync(null).AsTask());
		}

		[TestMethod]
		public async Task When_Null_Uri_Is_Queried()
		{
			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => Launcher.QueryUriSupportAsync(null, LaunchQuerySupportType.Uri).AsTask());
		}

		[TestMethod]
		public async Task When_LaunchUriAsync_From_Non_UI_Thread()
		{
			await Assert.ThrowsExceptionAsync<InvalidOperationException>(
				() => Task.Run(
					() => Launcher.LaunchUriAsync(new Uri("https://platform.uno"))));
		}

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

		[TestMethod]
		public async Task When_Settings_Uri_Is_Queried()
		{
			var result = await Launcher.QueryUriSupportAsync(
				new Uri("ms-settings:network-wifi"),
				LaunchQuerySupportType.Uri);

			Assert.AreEqual(LaunchQuerySupportStatus.Available, result);
		}

		[TestMethod]
		public async Task When_Unsupported_Special_Uri_Is_Queried()
		{
			var result = await Launcher.QueryUriSupportAsync(
				new Uri("ms-windows-store://home"),
				LaunchQuerySupportType.Uri);

			Assert.AreEqual(LaunchQuerySupportStatus.NotSupported, result);
		}

		private async Task DispatchAsync(Func<Task> asyncAction)
		{
			var completionSource = new TaskCompletionSource<object>();

			await CoreApplication.GetCurrentView().Dispatcher
				.RunAsync(CoreDispatcherPriority.Normal, async () =>
				{
					try
					{
						await asyncAction();
					}
					finally
					{
						completionSource.SetResult(null);
					}
				});

			await completionSource.Task;
		}
	}
}
#endif
