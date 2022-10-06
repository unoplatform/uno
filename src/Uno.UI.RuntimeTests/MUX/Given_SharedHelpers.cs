#if HAS_UNO
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Helpers.WinUI;

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
}
#endif
