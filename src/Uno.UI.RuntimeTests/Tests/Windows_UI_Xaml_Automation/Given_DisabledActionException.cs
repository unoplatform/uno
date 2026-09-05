using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	/// <summary>
	/// Runtime tests for XP-03: invoking a pattern method on a disabled element must throw
	/// <see cref="ElementNotEnabledException"/> (HRESULT UIA_E_ELEMENTNOTENABLED, 0x80040200),
	/// matching WinUI. Previously several peers silently no-op'd or threw the wrong exception type.
	/// These are WinUI-parity tests (ElementNotEnabledException is standard WinUI API).
	/// </summary>
	[TestClass]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public class Given_DisabledActionException
	{
		private const int UIA_E_ELEMENTNOTENABLED = unchecked((int)0x80040200);

		[TestMethod]
		[RunsOnUIThread]
		public void When_Disabled_Button_Invoke_Then_Throws()
		{
			var provider = (IInvokeProvider)CreatePeer(new Button { Content = "X", IsEnabled = false }, PatternInterface.Invoke);
			var ex = Assert.ThrowsExactly<ElementNotEnabledException>(() => provider.Invoke());
			Assert.AreEqual(UIA_E_ELEMENTNOTENABLED, ex.HResult, "HResult must be UIA_E_ELEMENTNOTENABLED");
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Disabled_RepeatButton_Invoke_Then_Throws()
		{
			var provider = (IInvokeProvider)CreatePeer(new RepeatButton { Content = "X", IsEnabled = false }, PatternInterface.Invoke);
			Assert.ThrowsExactly<ElementNotEnabledException>(() => provider.Invoke());
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Disabled_ToggleButton_Toggle_Then_Throws()
		{
			var provider = (IToggleProvider)CreatePeer(new ToggleButton { Content = "X", IsEnabled = false }, PatternInterface.Toggle);
			Assert.ThrowsExactly<ElementNotEnabledException>(() => provider.Toggle());
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Disabled_ToggleSwitch_Toggle_Then_Throws()
		{
			var provider = (IToggleProvider)CreatePeer(new ToggleSwitch { IsEnabled = false }, PatternInterface.Toggle);
			Assert.ThrowsExactly<ElementNotEnabledException>(() => provider.Toggle());
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Disabled_RadioButton_Select_Then_Throws()
		{
			var provider = (ISelectionItemProvider)CreatePeer(new RadioButton { Content = "X", IsEnabled = false }, PatternInterface.SelectionItem);
			Assert.ThrowsExactly<ElementNotEnabledException>(() => provider.Select());
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Enabled_Button_Invoke_Then_Does_Not_Throw()
		{
			var clicked = false;
			var button = new Button { Content = "X" };
			button.Click += (_, _) => clicked = true;
			await UITestHelper.Load(button);

			var provider = (IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.Invoke);
			provider.Invoke();

			Assert.IsTrue(clicked, "Invoking an enabled button must click it");
		}

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		public void When_Disabled_InvokeAutomationPeer_Then_Returns_False_Without_Throwing()
		{
			// InvokeAutomationPeer is the native (non-UIA) accessibility activation helper used by
			// Android/iOS/Skia-Android. A disabled element must report "not invoked" rather than
			// surfacing ElementNotEnabledException (which would crash those activation paths).
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(new Button { Content = "X", IsEnabled = false });
			Assert.IsNotNull(peer);
			Assert.IsFalse(peer.InvokeAutomationPeer(), "Disabled element must report not-invoked");
		}
#endif

		private static object CreatePeer(Control control, PatternInterface pattern)
		{
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control);
			Assert.IsNotNull(peer, "Control should have an automation peer");
			var provider = peer.GetPattern(pattern);
			Assert.IsNotNull(provider, $"Peer should expose the {pattern} pattern");
			return provider;
		}
	}
}
