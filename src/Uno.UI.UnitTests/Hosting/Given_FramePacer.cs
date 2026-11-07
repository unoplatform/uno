using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Hosting;

namespace Uno.UI.Tests.Hosting;

[TestClass]
public class Given_FramePacer
{
	[TestMethod]
	public void When_Frame_Requested_After_Dispose_Then_Ignored()
	{
		var elapsed = 0;
		var pacer = new FramePacer(60, () => Interlocked.Increment(ref elapsed));
		pacer.Dispose();

		pacer.RequestFrame();
		pacer.UpdateTargetFps(30);

		Thread.Sleep(100);
		Assert.AreEqual(0, elapsed);
	}

	[TestMethod]
	public async Task When_Disposed_While_Frames_Requested_From_Another_Thread_Then_No_Exception()
	{
		// X11 delivers Expose (-> RequestFrame) on its event thread while the window closes (-> Dispose) on another.
		for (var i = 0; i < 200; i++)
		{
			var pacer = new FramePacer(1000, () => { });
			using var started = new ManualResetEventSlim();
			var requester = Task.Run(() =>
			{
				started.Set();
				for (var j = 0; j < 1000; j++)
				{
					pacer.RequestFrame();
				}
			});

			started.Wait();
			pacer.Dispose();
			await requester;
		}
	}
}
