using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml.Automation.Peers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	[TestClass]
	public class Given_AutomationPeer
	{
		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetHeadingLevel()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetHeadingLevel();
			Assert.AreEqual(AutomationHeadingLevel.None, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_IsDialog()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.IsDialog();
			Assert.AreEqual(false, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetPattern()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetPattern(PatternInterface.Drag);
			Assert.AreEqual(null, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetAcceleratorKey()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetAcceleratorKey();
			Assert.AreEqual(string.Empty, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetAccessKey()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetAccessKey();
			Assert.AreEqual(string.Empty, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetAutomationControlType()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetAutomationControlType();
			Assert.AreEqual(AutomationControlType.Custom, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetAutomationId()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetAutomationId();
			Assert.AreEqual(string.Empty, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetBoundingRectangle()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetBoundingRectangle();
			Assert.AreEqual(default, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetChildren()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetChildren();
			Assert.AreEqual(null, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetClassName()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetClassName();
			Assert.AreEqual(string.Empty, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetClickablePoint()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetClickablePoint();
			Assert.AreEqual(default, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetHelpText()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetHelpText();
			Assert.AreEqual(string.Empty, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetItemStatus()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetItemStatus();
			Assert.AreEqual(string.Empty, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetItemType()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetItemType();
			Assert.AreEqual(string.Empty, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetLabeledBy()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetLabeledBy();
			Assert.AreEqual(null, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetLocalizedControlType()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetLocalizedControlType();
			Assert.AreEqual("custom", result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetName()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetName();
			Assert.AreEqual(string.Empty, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetOrientation()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetOrientation();
			Assert.AreEqual(AutomationOrientation.None, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_HasKeyboardFocus()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.HasKeyboardFocus();
			Assert.AreEqual(false, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_IsContentElement()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.IsContentElement();
			Assert.AreEqual(false, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_IsControlElement()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.IsControlElement();
			Assert.AreEqual(false, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_IsEnabled()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.IsEnabled();
			Assert.AreEqual(true, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_IsKeyboardFocusable()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.IsKeyboardFocusable();
			Assert.AreEqual(false, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_IsOffscreen()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.IsOffscreen();
			Assert.AreEqual(false, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_IsPassword()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.IsPassword();
			Assert.AreEqual(false, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_IsRequiredForForm()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.IsRequiredForForm();
			Assert.AreEqual(false, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_SetFocus()
		{
			var automationPeer = new TestAutomationPeer();
			// Should not throw
			automationPeer.SetFocus();
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetPeerFromPoint()
		{
			var automationPeer = new TestAutomationPeer();
#pragma warning disable CS0618 // Type or member is obsolete
			var result = automationPeer.GetPeerFromPoint(default);
#pragma warning restore CS0618 // Type or member is obsolete
			Assert.AreEqual(automationPeer, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetLiveSetting()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetLiveSetting();
			Assert.AreEqual(AutomationLiveSetting.Off, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_Navigate()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.Navigate(default);
			Assert.AreEqual(null, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetElementFromPoint()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetElementFromPoint(default);
			Assert.AreEqual(automationPeer, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetFocusedElement()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetFocusedElement();
			Assert.AreEqual(automationPeer, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_ShowContextMenu()
		{
			var automationPeer = new TestAutomationPeer();
			// Should not throw
			automationPeer.ShowContextMenu();
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetControlledPeers()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetControlledPeers();
			Assert.AreEqual(null, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetAnnotations()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetAnnotations();
			Assert.AreEqual(null, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetPositionInSet()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetPositionInSet();
			Assert.AreEqual(-1, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetSizeOfSet()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetSizeOfSet();
			Assert.AreEqual(-1, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetLevel()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetLevel();
			Assert.AreEqual(-1, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetLandmarkType()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetLandmarkType();
			Assert.AreEqual(AutomationLandmarkType.None, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetLocalizedLandmarkType()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetLocalizedLandmarkType();
			Assert.AreEqual(string.Empty, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_IsPeripheral()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.IsPeripheral();
			Assert.AreEqual(false, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_IsDataValidForForm()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.IsDataValidForForm();
			Assert.AreEqual(true, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetFullDescription()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetFullDescription();
			Assert.AreEqual(string.Empty, result);
		}

		private class TestAutomationPeer : AutomationPeer
		{
			public TestAutomationPeer()
			{
			}
		}
	}
}
