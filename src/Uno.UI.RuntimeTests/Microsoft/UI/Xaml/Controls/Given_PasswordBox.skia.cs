using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.RuntimeTests.Helpers;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_PasswordBox
{
	[TestMethod]
	public async Task When_Display_Text_Changes_Selection_Survives()
	{
		var SUT = new PasswordBox { Password = "0123456789", Width = 200 };
		await UITestHelper.Load(SUT);

		SUT.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		SUT.Core.Select(2, 5);
		await TestServices.WindowHelper.WaitForIdle();

		var displayBlock = SUT.Core.TextBoxView.DisplayBlock;
		var expected = new TextBlock.Range(2, 7);
		Assert.AreEqual(expected, displayBlock.Selection, "the engine must push its selection onto the display block");

		// Rewriting the mask must not clear the selection. The display block resets its own selection on a
		// text change only when no engine owns it — so this fails if it cannot see the PasswordBox's engine.
		SUT.PasswordChar = "#";
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(expected, displayBlock.Selection, "a display-text update must not clear the engine's selection");
	}

	[TestMethod]
	[DataRow(false, DisplayName = "Username focused first")]
	[DataRow(true, DisplayName = "Password focused first")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaIOS)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26610")]
	public async Task When_Login_Form_Then_AutoFill_Fills_Both_Inputs(bool passwordFirst)
	{
		var username = new TextBox { InputScope = new InputScope { Names = { new InputScopeName(InputScopeNameValue.EmailSmtpAddress) } } };
		var password = new PasswordBox();
		await UITestHelper.Load(new StackPanel { Children = { username, password } });

		try
		{
			(passwordFirst ? (Control)password : username).Focus(FocusState.Programmatic);
			await TestServices.WindowHelper.WaitForIdle();
			(passwordFirst ? (Control)username : password).Focus(FocusState.Programmatic);
			await TestServices.WindowHelper.WaitForIdle();

			// iOS AutoFill fills a login by making each of its native inputs the first responder in turn.
			var usernameView = NativeTextInputHelper.GetNativeView(username);
			var passwordView = NativeTextInputHelper.GetNativeView(password);
			Assert.IsTrue(NativeTextInputHelper.IsInHierarchy(usernameView), "the username must keep its native input");
			Assert.IsTrue(NativeTextInputHelper.IsInHierarchy(passwordView), "the password must keep its native input");
			Assert.AreEqual("username", NativeTextInputHelper.GetMember(usernameView!, "TextContentType")?.ToString());

			// iOS only finds a field that was never the first responder when it is on screen.
			var (x, y) = NativeTextInputHelper.GetFrameOrigin(passwordFirst ? passwordView! : usernameView!);
			Assert.IsTrue(x >= 0 && y >= 0, $"the input that isn't focused must be on screen, not at ({x}, {y})");

			NativeTextInputHelper.Invoke(usernameView!, "BecomeFirstResponder");
			NativeTextInputHelper.SetMember(usernameView!, "Text", "test@example.com");
			NativeTextInputHelper.Invoke(passwordView!, "BecomeFirstResponder");
			NativeTextInputHelper.SetMember(passwordView!, "Text", "Test1234!");
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual("test@example.com", username.Text);
			Assert.AreEqual("Test1234!", password.Password);
			Assert.AreNotEqual(FocusState.Unfocused, password.FocusState, "the focus must follow the native first responder");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaIOS)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26610")]
	public async Task When_Login_Form_Changes_Then_Native_Inputs_Follow()
	{
		var username = new TextBox();
		var password = new PasswordBox();
		var other = new TextBox();
		var form = new StackPanel { Children = { username, password } };
		await UITestHelper.Load(new StackPanel { Children = { form, other } });

		try
		{
			await FocusAsync(username);
			await FocusAsync(password);
			var usernameView = NativeTextInputHelper.GetNativeView(username);

			await FocusAsync(other);
			var otherView = NativeTextInputHelper.GetNativeView(other);
			Assert.IsTrue(NativeTextInputHelper.IsInHierarchy(otherView));
			await FocusAsync(password);

			Assert.IsFalse(NativeTextInputHelper.IsInHierarchy(otherView), "an input outside the login form must not keep its native input");
			Assert.AreSame(usernameView, NativeTextInputHelper.GetNativeView(username), "the username must keep the native input iOS may already track");
			Assert.IsTrue(NativeTextInputHelper.IsInHierarchy(usernameView));

			// Reapplying the template replaces the TextBoxView of the input, and its native input.
			var template = username.Template;
			username.Template = null;
			username.Template = template;
			await TestServices.WindowHelper.WaitForIdle();
			await FocusAsync(username);
			await FocusAsync(password);

			var reappliedUsernameView = NativeTextInputHelper.GetNativeView(username);
			Assert.AreNotSame(usernameView, reappliedUsernameView, "pre-condition: the template was reapplied");
			Assert.IsFalse(NativeTextInputHelper.IsInHierarchy(usernameView), "the native input of the former template must go away");
			Assert.IsTrue(NativeTextInputHelper.IsInHierarchy(reappliedUsernameView), "the username must be paired again");

			var passwordView = NativeTextInputHelper.GetNativeView(password);
			await FocusAsync(other);
			form.Children.Remove(username);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsFalse(NativeTextInputHelper.IsInHierarchy(reappliedUsernameView), "the native inputs must go away with the login form");
			Assert.IsFalse(NativeTextInputHelper.IsInHierarchy(passwordView));
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}

		static async Task FocusAsync(Control input)
		{
			input.Focus(FocusState.Programmatic);
			await TestServices.WindowHelper.WaitForIdle();
		}
	}
}
