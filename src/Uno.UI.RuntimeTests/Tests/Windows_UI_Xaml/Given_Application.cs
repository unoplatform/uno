using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	public class Given_Application
	{
		[TestMethod]
		public void When_Application_Is_Started_DispatcherShutdownMode_Defaults_To_OnLastWindowClose()
		{
	#if __SKIA__
			if (Application.Current.Host is null)
			{
				Assert.Inconclusive("Skia Islands use explicit shutdown semantics and are covered by other tests.");
			}
	#endif

			Assert.AreEqual(DispatcherShutdownMode.OnLastWindowClose, Application.Current.DispatcherShutdownMode);
		}
	}
}
