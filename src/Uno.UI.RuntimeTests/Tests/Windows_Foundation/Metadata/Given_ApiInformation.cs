using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_Foundation.Metadata
{
	[TestClass]
	public class Given_ApiInformation
	{
		[TestMethod]
		public void When_IsPropertyPresent_Called_Twice()
		{
			// Verifies fix for issue #4803
			// SharedHelpers called IsPropertyPresent twice for an implemented property
			// but the second call resulted in false

			// Application.Current is implemented on all targets
			var isPresent = ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Application", "Current");
			Assert.IsTrue(isPresent);
			var secondIsPresent = ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Application", "Current");
			Assert.IsTrue(secondIsPresent);
		}

		[TestMethod]
		public void When_StoreContract()
		{
			var isPresent = ApiInformation.IsApiContractPresent("Windows.Services.Store.StoreContract", 1);
			Assert.IsTrue(isPresent);
			isPresent = ApiInformation.IsApiContractPresent("Windows.Services.Store.StoreContract", 2, 3);
			Assert.IsTrue(isPresent);
			isPresent = ApiInformation.IsApiContractPresent("Windows.Services.Store.StoreContract", 4);
			Assert.IsTrue(isPresent);
			isPresent = ApiInformation.IsApiContractPresent("Windows.Services.Store.StoreContract", 100);
			Assert.IsFalse(isPresent);
		}
	}
}
