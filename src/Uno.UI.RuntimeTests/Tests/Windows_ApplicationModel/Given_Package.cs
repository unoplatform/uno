#if __IOS__ || __ANDROID__
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel;

namespace Uno.UI.RuntimeTests.Tests.Windows_ApplicationModel
{
	[TestClass]
	public class Given_Package
	{
		[TestMethod]
		public void When_DisplayNameQueried()
		{
			var SUT = Package.Current;
			Assert.IsNotNull(SUT.DisplayName);
		}
	}
}
#endif
