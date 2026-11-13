using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.TeachingTipTests
{
	[UITestClass]
	public class Given_TeachingTip_PlacementFallback
	{
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26429")]
		public void When_LeftTop_Then_Fallback_Order_Starts_With_Left_Right_Top()
		{
			var order = GetFallbackOrder(TeachingTipPlacementMode.LeftTop);

			Assert.AreEqual(TeachingTipPlacementMode.LeftTop, order[0]);
			Assert.AreEqual(TeachingTipPlacementMode.Left, order[1]);
			Assert.AreEqual(TeachingTipPlacementMode.Right, order[2]);
			Assert.AreEqual(TeachingTipPlacementMode.Top, order[3]);
		}

		private static TeachingTipPlacementMode[] GetFallbackOrder(TeachingTipPlacementMode preferred)
		{
			// The method only uses its argument, so a tip instance without running its constructor is enough.
			var tip = (TeachingTip)RuntimeHelpers.GetUninitializedObject(typeof(TeachingTip));
			var method = typeof(TeachingTip).GetMethod("GetPlacementFallbackOrder", BindingFlags.NonPublic | BindingFlags.Instance);
			return (TeachingTipPlacementMode[])method!.Invoke(tip, new object[] { preferred })!;
		}
	}
}
