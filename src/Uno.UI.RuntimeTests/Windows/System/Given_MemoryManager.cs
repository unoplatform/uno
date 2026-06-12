using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

		private void EnsureApiAvailable(string propertyName)
		{
			if (!Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent("Windows.System.MemoryManager, Uno", propertyName))
			{
				Assert.Inconclusive($"The Api {propertyName} is not implemented");
			}
		}
	}
}
