using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Uno.Extensions;
using Uno.UI.Xaml.Core;
using Private.Infrastructure;
using Uno.UI.DevTools.Input;
using Windows.UI.Input.Preview.Injection;

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

	// Like TextBox, a PasswordBox acts on a touch hold when the hold starts (taking the focus, which touch otherwise defers to
	// the release), and the delayed ContextRequested of the same hold (the text viewport is pannable) doesn't open a flyout.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22229")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop | RuntimeTestPlatforms.SkiaAndroid)] // Android convention: run on Desktop (dev) + real Android only
	public async Task When_Touch_Hold_Focuses_At_Hold_Start_Android()
	{
		var other = new Button { Content = "Other" };
		var SUT = new PasswordBox
		{
			Width = 300,
			Password = "secret words",
			TouchSelectionConvention = TextBoxCore.TouchTextSelectionConvention.Android
		};

		await UITestHelper.Load(new StackPanel { Children = { other, SUT } });
		other.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(FocusState.Unfocused, SUT.FocusState, "premise: the password box starts unfocused");

		var processor = VisualTree.GetContentRootForElement(SUT)?.InputManager?.ContextMenuProcessor
			?? throw new InvalidOperationException("ContextMenuProcessor should be available for this test.");
		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		try
		{
			finger.Press(SUT.GetAbsoluteBoundsRect().GetCenter());
			await TestServices.WindowHelper.WaitFor(() => SUT.FocusState != FocusState.Unfocused, timeoutMS: 5000, message: "the hold should focus the password box before the release");

			var delayedRequest = processor.GetContextMenuTimer() ?? throw new InvalidOperationException("the hold on the pannable text viewport should delay its ContextRequested");
			await TestServices.WindowHelper.WaitFor(() => !delayedRequest.IsEnabled, timeoutMS: 5000, message: "the delayed ContextRequested of the hold should be raised while still holding");
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsFalse(SUT.ContextFlyout?.IsOpen ?? false, "the delayed request must not open the context flyout");

			finger.Release();
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreNotEqual(FocusState.Unfocused, SUT.FocusState, "the password box must stay focused after the release");
		}
		finally
		{
			VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
		}
	}
}
