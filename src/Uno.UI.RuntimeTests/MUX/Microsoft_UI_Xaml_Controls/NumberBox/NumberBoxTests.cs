using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Controls;
using MUXControlsTestApp.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation;
using MUXControlsTestApp;
using Private.Infrastructure;
using System.Threading.Tasks;

#if !HAS_UNO_WINUI
using Windows.UI.Xaml.Controls;
#endif

namespace Uno.UI.RuntimeTests.MUX.Microsoft_UI_Xaml_Controls
{
	[TestClass]
	public class NumberBoxTests : MUXApiTestBase
	{
		[TestMethod]
		public void VerifyTextAlignmentPropagates()
		{
			var numberBox = SetupNumberBox();
			TextBox textBox = null;

			RunOnUIThread.Execute(() =>
			{
				Content.UpdateLayout();

				textBox = TestUtilities.FindDescendents<TextBox>(numberBox).Where(e => e.Name == "InputBox").Single();
				Assert.AreEqual(TextAlignment.Left, textBox.TextAlignment, "The default TextAlignment should be left.");

				numberBox.TextAlignment = TextAlignment.Right;
				Content.UpdateLayout();

				Assert.AreEqual(TextAlignment.Right, textBox.TextAlignment, "The TextAlignment should have been updated to Right.");
			});
		}

		[TestMethod]
		public void VerifyInputScopePropogates()
		{
			var numberBox = SetupNumberBox();

			RunOnUIThread.Execute(() =>
			{
				Content.UpdateLayout();
				var inputTextBox = TestUtilities.FindDescendents<TextBox>(numberBox).Where(e => e.Name == "InputBox").Single();

				Assert.HasCount(1, inputTextBox.InputScope.Names);
				Assert.AreEqual(InputScopeNameValue.Number, inputTextBox.InputScope.Names[0].NameValue, "The default InputScope should be 'Number'.");

				var scopeName = new InputScopeName();
				scopeName.NameValue = InputScopeNameValue.CurrencyAmountAndSymbol;
				var scope = new InputScope();
				scope.Names.Add(scopeName);

				numberBox.InputScope = scope;
				Content.UpdateLayout();

				Assert.HasCount(1, inputTextBox.InputScope.Names);
				Assert.AreEqual(InputScopeNameValue.CurrencyAmountAndSymbol, inputTextBox.InputScope.Names[0].NameValue, "The InputScope should be 'CurrencyAmountAndSymbol'.");
			});

			return;
		}

		[TestMethod]
		public async Task VerifyIsEnabledChangeUpdatesVisualState()
		{
			var numberBox = SetupNumberBox();

			VisualStateGroup commonStatesGroup = null;
			RunOnUIThread.Execute(() =>
			{
				// Check 1: Set IsEnabled to true.
				numberBox.IsEnabled = true;
				Content.UpdateLayout();

				var numberBoxLayoutRoot = (FrameworkElement)VisualTreeHelper.GetChild(numberBox, 0);
				commonStatesGroup = VisualStateManager.GetVisualStateGroups(numberBoxLayoutRoot).First(vsg => vsg.Name.Equals("CommonStates"));

				Assert.AreEqual("Normal", commonStatesGroup.CurrentState.Name);

				// Check 2: Set IsEnabled to false.
				numberBox.IsEnabled = false;
			});
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Assert.AreEqual("Disabled", commonStatesGroup.CurrentState.Name);

				// Check 3: Set IsEnabled back to true.
				numberBox.IsEnabled = true;
			});
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Assert.AreEqual("Normal", commonStatesGroup.CurrentState.Name);
			});
		}

		[TestMethod]
		public async Task VerifyUIANameBehavior()
		{
			NumberBox numberBox = null;
			TextBox textBox = null;

			RunOnUIThread.Execute(() =>
			{
				numberBox = new NumberBox();
				Content = numberBox;
				Content.UpdateLayout();

				textBox = TestPage.FindVisualChildrenByType<TextBox>(numberBox)[0];
				Assert.IsNotNull(textBox);
				numberBox.Header = "Some header";
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				VerifyUIAName("Some header");
				numberBox.Header = new Button();
				AutomationProperties.SetName(numberBox, "Some UIA name");
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				VerifyUIAName("Some UIA name");
				numberBox.Header = new Button();
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				VerifyUIAName("Some UIA name");
				numberBox.Minimum = 0;
				numberBox.Maximum = 10;
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				VerifyUIAName("Some UIA name Minimum0 Maximum10");
				numberBox.Minimum = 50;
				numberBox.Maximum = 100;
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				VerifyUIAName("Some UIA name Minimum50 Maximum100");
			});

			void VerifyUIAName(string value)
			{
				Assert.AreEqual(value, FrameworkElementAutomationPeer.CreatePeerForElement(textBox).GetName());
			}
		}

		private NumberBox SetupNumberBox()
		{
			NumberBox numberBox = null;
			RunOnUIThread.Execute(() =>
			{
				numberBox = new NumberBox();
				Content = numberBox;
			});

			return numberBox;
		}

	}
}
