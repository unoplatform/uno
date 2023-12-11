#if HAS_UNO
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.Helpers.WinUI;
using Uno.UI.Xaml;

namespace Uno.UI.RuntimeTests.MUX;

[TestClass]
public class Given_SharedHelpers
{
	[TestMethod]
	public void When_Available_Contract_Requested()
	{
		Assert.IsTrue(SharedHelpers.IsAPIContractVxAvailable(1));
	}

	[TestMethod]
	public void When_Unavailable_Contract_Requested()
	{
		Assert.IsFalse(SharedHelpers.IsAPIContractVxAvailable(1000));
	}

	[TestMethod]
	public void When_Contract_Requested_In_Ascending_Order()
	{
		Assert.IsTrue(SharedHelpers.IsAPIContractVxAvailable(1));
		Assert.IsFalse(SharedHelpers.IsAPIContractVxAvailable(1000));
	}

	[TestMethod]
	public void When_Contract_Requested_In_Descending_Order()
	{
		Assert.IsFalse(SharedHelpers.IsAPIContractVxAvailable(1000));
		Assert.IsTrue(SharedHelpers.IsAPIContractVxAvailable(1));
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ScheduleActionAfterWait()
	{
		// Try scheduling a wait action 100ms and assert it happened
		var actionTriggered = false;
		SharedHelpers.ScheduleActionAfterWait(() => actionTriggered = true, 100);
		Assert.IsFalse(actionTriggered);

		await TestServices.WindowHelper.WaitFor(() => actionTriggered);
	}
}
#endif
