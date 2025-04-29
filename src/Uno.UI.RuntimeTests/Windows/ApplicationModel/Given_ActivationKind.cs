using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;

namespace Uno.UI.RuntimeTests.Tests.Windows_ApplicationModel;

[TestClass]
public class Given_ActivationKind
{
	[TestMethod]
	public void When_ExtendedActivationKind_Values_Match()
	{
		foreach (var kind in Enum.GetValues(typeof(ActivationKind)))
		{
			var extendedKind = (ExtendedActivationKind)(int)kind;
			Assert.IsTrue(
				Enum.IsDefined(typeof(ExtendedActivationKind), extendedKind),
				$"ActivationKind.{kind} has no matching definition in ExtendedActivationKind");
			var kindName = Enum.GetName(typeof(ActivationKind), kind);
			var extendedKindName = Enum.GetName(typeof(ExtendedActivationKind), extendedKind);
			Assert.AreEqual(kindName, extendedKindName, $"Mismatch for ActivationKind.{kindName} (ExtendedActivationKind.{extendedKind}");
		}
	}
}
