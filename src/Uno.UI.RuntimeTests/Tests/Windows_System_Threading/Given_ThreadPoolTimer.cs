using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.Core;
using Windows.System;
using Windows.System.Threading;
using Windows.UI.Core;

namespace Uno.UI.RuntimeTests.Tests.Windows_System
{
	[TestClass]
	public class Given_ThreadPoolTimer
	{
		[TestMethod]
		public async Task When_Timer()
		{
			var handlerCount = 0;
			var timer = ThreadPoolTimer.CreateTimer(_ => handlerCount++, TimeSpan.FromMilliseconds(100));

			await Task.Delay(500);

			Assert.AreEqual(handlerCount, 1);

			await Task.Delay(500);

			Assert.AreEqual(handlerCount, 1);
		}

		[TestMethod]
		public async Task When_PeriodicTimer()
		{
			var handlerCount = 0;
			var timer = ThreadPoolTimer.CreatePeriodicTimer(_ => handlerCount++, TimeSpan.FromMilliseconds(100));

			await Task.Delay(500);

			Assert.IsTrue(handlerCount > 1);
		}
	}
}
