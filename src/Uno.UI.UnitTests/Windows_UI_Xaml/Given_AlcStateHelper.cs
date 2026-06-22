using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml.Core;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_AlcStateHelper
	{
		[TestMethod]
		public void When_Alive_Then_Not_UnloadInitiated_And_When_Unloaded_Then_UnloadInitiated()
		{
			if (!RuntimeFeature.IsDynamicCodeSupported)
			{
				Assert.Inconclusive("Collectible AssemblyLoadContext is not supported on this target.");
			}

			var alc = new AssemblyLoadContext("AlcStateHelperProbe", isCollectible: true);

			// valueIfUnknown is set to the OPPOSITE of the expected result so a failed read (e.g. the
			// reflected _state field no longer resolving) would surface as a test failure, not a false pass.
			Assert.IsFalse(
				AlcStateHelper.IsUnloadInitiated(alc, valueIfUnknown: true),
				"A live ALC must report that its unload has not been initiated.");

			alc.Unload();

			Assert.IsTrue(
				AlcStateHelper.IsUnloadInitiated(alc, valueIfUnknown: false),
				"An ALC whose Unload() has been called must report that its unload has been initiated.");
		}
	}
}
