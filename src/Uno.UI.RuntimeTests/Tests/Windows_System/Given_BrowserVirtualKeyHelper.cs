using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.System;

#if HAS_UNO
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

		// W3C event.code values for the OEM-row physical keys map to the raw Win32
		// VK_OEM_* values. Mapping is shift-independent because event.code identifies
		// the physical key location, not the produced character.
		[TestMethod]
		[DataRow("Semicolon", 0xBA)]     // VK_OEM_1
		[DataRow("Equal", 0xBB)]         // VK_OEM_PLUS
		[DataRow("Comma", 0xBC)]         // VK_OEM_COMMA
		[DataRow("Minus", 0xBD)]         // VK_OEM_MINUS
		[DataRow("Period", 0xBE)]        // VK_OEM_PERIOD
		[DataRow("Slash", 0xBF)]         // VK_OEM_2
		[DataRow("Backquote", 0xC0)]     // VK_OEM_3
		[DataRow("BracketLeft", 0xDB)]   // VK_OEM_4
		[DataRow("Backslash", 0xDC)]     // VK_OEM_5
		[DataRow("BracketRight", 0xDD)]  // VK_OEM_6
		[DataRow("Quote", 0xDE)]         // VK_OEM_7
		[DataRow("IntlBackslash", 0xE2)] // VK_OEM_102
		[RunsOnUIThread]
		public void When_FromCode_OemRowCode_Returns_OemVirtualKey(string code, int expectedVirtualKey)
		{
			var result = BrowserVirtualKeyHelper.FromCode(code);
			Assert.AreEqual(expectedVirtualKey, (int)result, $"{code} should map to VirtualKey 0x{expectedVirtualKey:X2}");
		}
	}
}
#endif
