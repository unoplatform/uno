using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.System;

namespace Uno.UI.RuntimeTests.Tests.Windows_System
{
	[TestClass]
	public class Given_MemoryManager
	{
		[TestMethod]
		public void When_AppMemoryUsage()
		{
			EnsureApiAvailable("AppMemoryUsage");

			Assert.AreNotEqual<ulong>(0, MemoryManager.AppMemoryUsage);
		}

		[TestMethod]
		public void When_AppMemoryUsageLimit()
		{
			EnsureApiAvailable("AppMemoryUsageLimit");

			Assert.AreNotEqual<ulong>(0, MemoryManager.AppMemoryUsageLimit);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24871")]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.SkiaUIKit)]
		public void When_Probed_By_Assembly_Name()
		{
			Assert.IsTrue(Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent("Windows.System.MemoryManager, Uno.WinRT", "AppMemoryUsage"));
		}

#if HAS_UNO
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24871")]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.SkiaUIKit)]
		public void When_Pools_Check_MemoryManager()
		{
			Assert.IsTrue(new Uno.Buffers.DefaultArrayPoolPlatformProvider().CanUseMemoryManager, "ArrayPool");
			Assert.IsTrue(new Microsoft.UI.Xaml.FrameworkTemplatePoolDefaultPlatformProvider().CanUseMemoryManager, "FrameworkTemplatePool");
		}
#endif

		private void EnsureApiAvailable(string propertyName)
		{
			if (!Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent("Windows.System.MemoryManager, Uno.WinRT", propertyName))
			{
				Assert.Inconclusive($"The Api {propertyName} is not implemented");
			}
		}
	}
}
