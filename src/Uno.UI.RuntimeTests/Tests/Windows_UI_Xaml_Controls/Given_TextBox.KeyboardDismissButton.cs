#if HAS_UNO
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using TextBoxExtensions = Uno.UI.Xaml.Controls.TextBoxExtensions;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_TextBox
	{
		// The toolbar itself is native (iOS) and cannot be observed from a runtime test - these cover the
		// managed contract the native side reads: the default, inheritance, and local overrides.
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24384")]
		public void When_ShowKeyboardDismissButton_Not_Set_Then_False()
		{
			Assert.IsFalse(TextBoxExtensions.GetShowKeyboardDismissButton(new TextBox()));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24384")]
		public async Task When_ShowKeyboardDismissButton_Set_On_Container_Then_Inherited()
		{
			try
			{
				var textBox = new TextBox();
				var passwordBox = new PasswordBox();
				var optedOut = new TextBox();
				TextBoxExtensions.SetShowKeyboardDismissButton(optedOut, false);

				var panel = new StackPanel();
				panel.Children.Add(textBox);
				panel.Children.Add(passwordBox);
				panel.Children.Add(optedOut);
				TextBoxExtensions.SetShowKeyboardDismissButton(panel, true);

				await UITestHelper.Load(panel);

				Assert.IsTrue(TextBoxExtensions.GetShowKeyboardDismissButton(textBox));
				Assert.IsTrue(TextBoxExtensions.GetShowKeyboardDismissButton(passwordBox));
				Assert.IsFalse(TextBoxExtensions.GetShowKeyboardDismissButton(optedOut));
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24384")]
		public async Task When_ShowKeyboardDismissButton_Toggled_While_Focused()
		{
			try
			{
				var SUT = new TextBox();
				await UITestHelper.Load(SUT);

				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				TextBoxExtensions.SetShowKeyboardDismissButton(SUT, true);
				await WindowHelper.WaitForIdle();
				Assert.IsTrue(TextBoxExtensions.GetShowKeyboardDismissButton(SUT));

				TextBoxExtensions.SetShowKeyboardDismissButton(SUT, false);
				await WindowHelper.WaitForIdle();
				Assert.IsFalse(TextBoxExtensions.GetShowKeyboardDismissButton(SUT));
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}
}
#endif
