using System;
using System.Threading.Tasks;
using Private.Infrastructure;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Input
{
	/// <summary>
	/// WinUI parity: focus interactions with elements that are not (or no longer) in the live visual tree.
	/// On WinUI, CUIElement::IsFocusable() requires IsActive() (element in the live tree), so Focus() on a
	/// detached element fails and focus-candidate searches skip detached subtrees.
	/// </summary>
	[TestClass]
	public class Given_FocusManager_DetachedElements
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Focus_Detached_Element_Should_Fail()
		{
			var okButton = new Button { Content = "OK" };
			var dialogPanel = new StackPanel { Children = { okButton } };
			var topBarButton = new Button { Content = "TopBar" };
			var root = new StackPanel { Children = { topBarButton, dialogPanel } };

			TestServices.WindowHelper.WindowContent = root;
			await TestServices.WindowHelper.WaitForLoaded(root);

			Assert.IsTrue(okButton.Focus(FocusState.Programmatic));
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual(okButton, FocusManager.GetFocusedElement(root.XamlRoot));

			root.Children.Remove(dialogPanel);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreNotEqual(okButton, FocusManager.GetFocusedElement(root.XamlRoot), "Focus must move off an element that left the visual tree");

			var focusedAfterDetach = FocusManager.GetFocusedElement(root.XamlRoot);

			var refocusResult = okButton.Focus(FocusState.Programmatic);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsFalse(refocusResult, "Focus() must fail for an element outside the live visual tree");
			Assert.AreEqual(focusedAfterDetach, FocusManager.GetFocusedElement(root.XamlRoot), "Focused element must not change when focusing a detached element");
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_FindFirstFocusableElement_On_Detached_Subtree_Should_Be_Null()
		{
			var okButton = new Button { Content = "OK" };
			var dialogPanel = new StackPanel { Children = { okButton } };
			var topBarButton = new Button { Content = "TopBar" };
			var root = new StackPanel { Children = { topBarButton, dialogPanel } };

			TestServices.WindowHelper.WindowContent = root;
			await TestServices.WindowHelper.WaitForLoaded(root);

			Assert.IsNotNull(FocusManager.FindFirstFocusableElement(dialogPanel));

			root.Children.Remove(dialogPanel);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNull(FocusManager.FindFirstFocusableElement(dialogPanel), "A detached subtree must not yield focus candidates");
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Focus_Stolen_By_Detached_Element_Tab_Should_Stay_In_Cycle_Scope()
		{
			var okButton = new Button { Content = "OK" };
			var cyclePanel = new StackPanel
			{
				TabFocusNavigation = KeyboardNavigationMode.Cycle,
				Children = { okButton }
			};
			var topBarButton1 = new Button { Content = "TopBar1" };
			var topBarButton2 = new Button { Content = "TopBar2" };
			var strayButton = new Button { Content = "Stray" };
			var strayPanel = new StackPanel { Children = { strayButton } };
			var root = new StackPanel { Children = { topBarButton1, topBarButton2, strayPanel, cyclePanel } };

			TestServices.WindowHelper.WindowContent = root;
			await TestServices.WindowHelper.WaitForLoaded(root);

			Assert.IsTrue(okButton.Focus(FocusState.Programmatic));
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual(okButton, FocusManager.GetFocusedElement(root.XamlRoot));

			// Simulate an app behavior (e.g. a deferred auto-focus) focusing an element
			// of a view that has already been removed from the tree. On WinUI this is a no-op.
			root.Children.Remove(strayPanel);
			await TestServices.WindowHelper.WaitForIdle();
			strayButton.Focus(FocusState.Programmatic);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(okButton, FocusManager.GetFocusedElement(root.XamlRoot), "Focus must remain on the dialog button");

			await TestServices.KeyboardHelper.Tab();
			await TestServices.WindowHelper.WaitForIdle();

			// The Cycle scope contains a single focusable element, so Tab must wrap back to it.
			Assert.AreEqual(okButton, FocusManager.GetFocusedElement(root.XamlRoot), "Tab must stay inside the TabFocusNavigation=Cycle scope");
		}
	}
}
