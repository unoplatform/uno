#if HAS_UNO
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Uno.Helpers;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Uno_Helpers;

[TestClass]
[RunsOnUIThread]
public class Given_DeferralManager
{
	[TestMethod]
	public void When_Completed_Synchronously_After_Event_Raise()
	{
		var deferralManager = new DeferralManager<Deferral>(h => new Deferral(h));
		Assert.IsTrue(deferralManager.EventRaiseCompleted());
		Assert.IsTrue(deferralManager.CompletedSynchronously);
	}

	[TestMethod]
	public void When_Completed_Synchronously_In_Event_Handler()
	{
		var deferralManager = new DeferralManager<Deferral>(h => new Deferral(h));
		deferralManager.Completed += (s, e) =>
		{
			Assert.IsTrue(deferralManager.CompletedSynchronously);
		};
		deferralManager.EventRaiseCompleted();
	}

	[TestMethod]
	public void When_Completed_Synchronously_With_Requests()
	{
		var deferralManager = new DeferralManager<Deferral>(h => new Deferral(h));
		var deferral1 = deferralManager.GetDeferral();
		var deferral2 = deferralManager.GetDeferral();
		deferral2.Complete();
		deferral1.Complete();
		deferralManager.Completed += (s, e) =>
		{
			Assert.IsTrue(deferralManager.CompletedSynchronously);
		};
		Assert.IsTrue(deferralManager.EventRaiseCompleted());
		Assert.IsTrue(deferralManager.CompletedSynchronously);
	}

	[TestMethod]
	public async Task When_Completed_Asynchronously_With_Requests()
	{
		var deferralManager = new DeferralManager<Deferral>(h => new Deferral(h));
		var deferral1 = deferralManager.GetDeferral();
		var deferral2 = deferralManager.GetDeferral();

		bool lastDeferralCompleting = false;
		deferralManager.Completed += (s, e) =>
		{
			Assert.IsTrue(lastDeferralCompleting);
			Assert.IsFalse(deferralManager.CompletedSynchronously);
		};
		var completedSynchronously = deferralManager.EventRaiseCompleted();
		Assert.IsFalse(completedSynchronously);
		Assert.IsFalse(deferralManager.CompletedSynchronously);

		await Task.Yield();

		deferral2.Complete();

		Assert.IsFalse(deferralManager.CompletedSynchronously);

		await Task.Yield();

		Assert.IsFalse(deferralManager.CompletedSynchronously);

		lastDeferralCompleting = true;
		deferral1.Complete();

		Assert.IsFalse(deferralManager.CompletedSynchronously);
	}

	[TestMethod]
	public void When_Incomplete_Deferral()
	{
		var deferralManager = new DeferralManager<Deferral>(h => new Deferral(h));

		var deferral = deferralManager.GetDeferral();

		bool testFinished = false;
		deferralManager.Completed += (s, e) =>
		{
			if (!testFinished)
			{
				Assert.Fail("Should not be called");
			}
		};

		var completedSynchronously = deferralManager.EventRaiseCompleted();

		// Set to avoid ghost assertion failure (deferral disposal will complete it).
		testFinished = true;
	}

	[TestMethod]
	public void When_Many_Deferrals()
	{
		var deferralManager = new DeferralManager<Deferral>(h => new Deferral(h));

		var random = new Random(42);

		var deferrals = new List<Deferral>();
		for (int i = 0; i < 20; i++)
		{
			deferrals.Add(deferralManager.GetDeferral());
		}

		bool isCompleted = false;
		deferralManager.Completed += (s, e) =>
		{
			isCompleted = true;
		};

		deferralManager.EventRaiseCompleted();

		while (deferrals.Count > 0)
		{
			Assert.IsFalse(isCompleted);
			var randomIndex = random.Next(deferrals.Count);
			var randomDeferral = deferrals[randomIndex];
			randomDeferral.Complete();
			deferrals.RemoveAt(randomIndex);
		}

		Assert.IsTrue(isCompleted);
	}

	[TestMethod]
	public void When_Deferrals_Disposed()
	{
		var deferralManager = new DeferralManager<Deferral>(h => new Deferral(h));

		var deferral1 = deferralManager.GetDeferral();
		var deferral2 = deferralManager.GetDeferral();

		bool isCompleted = false;
		deferralManager.Completed += (s, e) =>
		{
			isCompleted = true;
		};

		deferralManager.EventRaiseCompleted();

		Assert.IsFalse(isCompleted);

		deferral1.Dispose();

		Assert.IsFalse(isCompleted);

		deferral2.Dispose();

		Assert.IsTrue(isCompleted);
	}
}
#endif
