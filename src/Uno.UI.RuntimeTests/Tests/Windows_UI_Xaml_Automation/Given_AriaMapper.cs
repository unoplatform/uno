using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO
using Uno.UI.Runtime.Skia;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	/// <summary>
	/// Runtime tests for the shared <see cref="AriaMapper"/> role/attribute mapping consumed by the
	/// WebAssembly (ARIA) and macOS (native role) backends. Covers XP-01 of the a11y parity
	/// remediation: control-type -> role table gaps and aria-haspopup for menu-flyout buttons.
	/// </summary>
	[TestClass]
	public class Given_AriaMapper
	{
#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		[DataRow(AutomationControlType.MenuBar, "menubar")]
		[DataRow(AutomationControlType.Table, "table")]
		[DataRow(AutomationControlType.Separator, "separator")]
		[DataRow(AutomationControlType.Menu, "menu")]
		[DataRow(AutomationControlType.MenuItem, "menuitem")]
		public void When_GetAriaRole_Then_ControlTypeMapped(AutomationControlType controlType, string expected)
		{
			// MenuBar/Table/Separator were missing from ControlTypeToRoleMap even though the peers
			// return those control types (peers returned the type, the mapper dropped it).
			Assert.AreEqual(expected, AriaMapper.GetAriaRole(controlType));
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_DropDownButton_Then_HasPopup_Menu()
		{
			var control = new DropDownButton { Content = "Menu" };
			await UITestHelper.Load(control);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control);
			Assert.IsNotNull(peer);

			var attributes = AriaMapper.GetAriaAttributes(peer);
			Assert.AreEqual("menu", attributes.HasPopup, "DropDownButton should expose aria-haspopup=menu");
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_SplitButton_Then_HasPopup_Menu()
		{
			var control = new SplitButton { Content = "Action" };
			await UITestHelper.Load(control);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control);
			Assert.IsNotNull(peer);

			var attributes = AriaMapper.GetAriaAttributes(peer);
			Assert.AreEqual("menu", attributes.HasPopup, "SplitButton should expose aria-haspopup=menu");
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Expander_Then_Expanded_Without_HasPopup()
		{
			// Regression guard: Expander reports the Button control type AND exposes the
			// ExpandCollapse pattern (for inline expand/collapse), so a control-type-based
			// haspopup rule would wrongly tag it as a menu button. It must expose aria-expanded
			// without aria-haspopup.
			var control = new Expander { Header = "Header", Content = "Content" };
			await UITestHelper.Load(control);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control);
			Assert.IsNotNull(peer);

			var attributes = AriaMapper.GetAriaAttributes(peer);
			Assert.IsNull(attributes.HasPopup, "Expander must not expose aria-haspopup");
			Assert.IsNotNull(attributes.Expanded, "Expander should still expose aria-expanded");
		}
#endif
	}
}
