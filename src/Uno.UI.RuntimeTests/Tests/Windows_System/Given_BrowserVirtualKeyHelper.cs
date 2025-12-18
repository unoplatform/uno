using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.System;

namespace Uno.UI.RuntimeTests.Tests.Windows_System
{
	[TestClass]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Wasm)]
	public class Given_BrowserVirtualKeyHelper
	{
		[TestMethod]
		[RunsOnUIThread]
		public void When_FromCode_ControlLeft_Returns_Control()
		{
			var result = BrowserVirtualKeyHelper.FromCode("ControlLeft");
			Assert.AreEqual(VirtualKey.Control, result, "ControlLeft should map to VirtualKey.Control");
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_FromCode_ControlRight_Returns_Control()
		{
			var result = BrowserVirtualKeyHelper.FromCode("ControlRight");
			Assert.AreEqual(VirtualKey.Control, result, "ControlRight should map to VirtualKey.Control");
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_FromCode_ShiftLeft_Returns_Shift()
		{
			var result = BrowserVirtualKeyHelper.FromCode("ShiftLeft");
			Assert.AreEqual(VirtualKey.Shift, result, "ShiftLeft should map to VirtualKey.Shift");
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_FromCode_ShiftRight_Returns_Shift()
		{
			var result = BrowserVirtualKeyHelper.FromCode("ShiftRight");
			Assert.AreEqual(VirtualKey.Shift, result, "ShiftRight should map to VirtualKey.Shift");
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_FromCode_AltLeft_Returns_Menu()
		{
			var result = BrowserVirtualKeyHelper.FromCode("AltLeft");
			Assert.AreEqual(VirtualKey.Menu, result, "AltLeft should map to VirtualKey.Menu");
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_FromCode_AltRight_Returns_Menu()
		{
			var result = BrowserVirtualKeyHelper.FromCode("AltRight");
			Assert.AreEqual(VirtualKey.Menu, result, "AltRight should map to VirtualKey.Menu");
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_FromCode_MetaLeft_Returns_RightWindows()
		{
			// Note: The mapping appears swapped (MetaLeft->RightWindows, MetaRight->LeftWindows)
			// This is pre-existing behavior in BrowserVirtualKeyHelper.FromCode()
			var result = BrowserVirtualKeyHelper.FromCode("MetaLeft");
			Assert.AreEqual(VirtualKey.RightWindows, result, "MetaLeft currently maps to VirtualKey.RightWindows");
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_FromCode_MetaRight_Returns_LeftWindows()
		{
			// Note: The mapping appears swapped (MetaLeft->RightWindows, MetaRight->LeftWindows)
			// This is pre-existing behavior in BrowserVirtualKeyHelper.FromCode()
			var result = BrowserVirtualKeyHelper.FromCode("MetaRight");
			Assert.AreEqual(VirtualKey.LeftWindows, result, "MetaRight currently maps to VirtualKey.LeftWindows");
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_FromCode_Space_Returns_Space()
		{
			var result = BrowserVirtualKeyHelper.FromCode("Space");
			Assert.AreEqual(VirtualKey.Space, result, "Space should map to VirtualKey.Space");
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_FromCode_KeyA_Returns_A()
		{
			var result = BrowserVirtualKeyHelper.FromCode("KeyA");
			Assert.AreEqual(VirtualKey.A, result, "KeyA should map to VirtualKey.A");
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_FromCode_Digit0_Returns_Number0()
		{
			var result = BrowserVirtualKeyHelper.FromCode("Digit0");
			Assert.AreEqual(VirtualKey.Number0, result, "Digit0 should map to VirtualKey.Number0");
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_FromCode_UnknownKey_Returns_None()
		{
			var result = BrowserVirtualKeyHelper.FromCode("UnknownKey123");
			Assert.AreEqual(VirtualKey.None, result, "Unknown key codes should map to VirtualKey.None");
		}
	}
}
