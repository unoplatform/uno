using System;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;

namespace Uno.UI.RuntimeTests.Tests.Windows_ApplicationModel;

[TestClass]
public class Given_ActivationKind
{
	[TestMethod]
	public void When_ExtendedActivationKind_Values_Match()
	{
		// Neither enum is a superset: the Windows SDK and WinAppSDK each add kinds the other lacks,
		// and the WinAppSDK head projects ActivationKind from an older SDK.
		var sharedCount = 0;
		foreach (var extendedKind in Enum.GetValues<ExtendedActivationKind>())
		{
			var kind = (ActivationKind)(int)extendedKind;
			if (!Enum.IsDefined(kind))
			{
				continue;
			}

			sharedCount++;
			Assert.AreEqual(extendedKind.ToString(), kind.ToString(), $"Mismatch for ExtendedActivationKind.{extendedKind} (ActivationKind.{kind})");
		}

		Assert.IsGreaterThan(0, sharedCount);
	}
}
