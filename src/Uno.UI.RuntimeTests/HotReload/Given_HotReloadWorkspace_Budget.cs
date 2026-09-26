#if HAS_UNO
#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.Testing;

namespace Uno.UI.RuntimeTests.Tests.HotReload;

[TestClass]
public class Given_HotReloadWorkspace_Budget
{
	[TestMethod]
	public async Task When_Run_Outlives_Budget_Then_Failure_Is_Not_Retryable()
	{
		var failure = await Assert.ThrowsExactlyAsync<NonRetryableTestFailureException>(
			() => Given_HotReloadWorkspace.RunWithinBudget(
				async ct =>
				{
					await Task.Delay(Timeout.Infinite, ct);
					return "unreachable";
				},
				TimeSpan.FromMilliseconds(50),
				"wedged"));

		Assert.AreEqual("wedged", failure.Message);
	}

	[TestMethod]
	public async Task When_Run_Cancels_Itself_Then_Cancellation_Propagates()
	{
		await Assert.ThrowsExactlyAsync<OperationCanceledException>(
			() => Given_HotReloadWorkspace.RunWithinBudget<string>(
				_ => throw new OperationCanceledException(),
				TimeSpan.FromMinutes(1),
				"wedged"));
	}

	[TestMethod]
	public async Task When_Run_Completes_Within_Budget_Then_Result_Is_Returned()
	{
		var result = await Given_HotReloadWorkspace.RunWithinBudget(
			_ => Task.FromResult("done"),
			TimeSpan.FromMinutes(1),
			"wedged");

		Assert.AreEqual("done", result);
	}
}
#endif
