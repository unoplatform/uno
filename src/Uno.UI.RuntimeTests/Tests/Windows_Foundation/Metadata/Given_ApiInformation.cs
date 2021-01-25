using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_Foundation.Metadata
{
	[TestClass]
	public class Given_ApiInformation
    {
		[TestMethod]
		public async Task When_IsPropertyPresent_Called_Twice()
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
	}
}
