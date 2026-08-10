using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Uno.UI.Helpers;
using Uno.UI.RuntimeTests.Helpers;
using Windows.System;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public partial class Given_PasswordBox
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_PasswordChar_Visual_Comparison()
	{
		// Test password with 4 characters 
		const string testPassword = "test";
		const int passwordLength = 4;

		// Create a PasswordBox with PasswordChar="A"
		var passwordBox = new PasswordBox
		{
			PasswordChar = "A",
			Password = testPassword,
			FontSize = 16,
			Width = 100,
			Height = 32,
			Padding = new Thickness(4)
		};

		// Create a TextBox with "AAAA" to compare visual appearance. Spell-check is off so the comparison
		// isolates the mask character: a PasswordBox never spell-checks, and a TextBox showing the same
		// letters otherwise draws a squiggly underline the PasswordBox correctly lacks.
		var textBox = new TextBox
		{
			Text = new string('A', passwordLength),
			IsSpellCheckEnabled = false,
			FontSize = 16,
			Width = 100,
			Height = 32,
			Padding = new Thickness(4)
		};

		var parent = new StackPanel()
		{
			Margin = new Thickness(10),
			Spacing = 8
		};

		parent.Children.Add(passwordBox);
		parent.Children.Add(textBox);

		// Load and take screenshot
		await UITestHelper.Load(parent);
		var passwordBoxScreenshot = await UITestHelper.ScreenShot(passwordBox);
		var textBoxScreenshot = await UITestHelper.ScreenShot(textBox);

		// Compare that PasswordBox with PasswordChar="A" looks similar to TextBox with "AAAA"
		await ImageAssert.AreSimilarAsync(passwordBoxScreenshot, textBoxScreenshot, imperceptibilityThreshold: 0.05);

		// Now change PasswordChar to "B" and verify it changes
		passwordBox.PasswordChar = "B";
		await UITestHelper.WaitForIdle();

		// Take new screenshot of PasswordBox with "B" characters
		var passwordBoxBScreenshot = await UITestHelper.ScreenShot(passwordBox);

		// Update TextBox to show "BBBB" for comparison
		textBox.Text = new string('B', passwordLength);
		await UITestHelper.WaitForIdle();
		var textBoxBScreenshot = await UITestHelper.ScreenShot(textBox);

		// Verify PasswordBox with PasswordChar="B" looks similar to TextBox with "BBBB"
		await ImageAssert.AreSimilarAsync(passwordBoxBScreenshot, textBoxBScreenshot, imperceptibilityThreshold: 0.05);

		// Verify that the two PasswordBox screenshots are different (A vs B)
		await ImageAssert.AreNotEqualAsync(passwordBoxScreenshot, passwordBoxBScreenshot);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_PasswordChar_Special_Characters()
	{
		// Test with various special characters
		const string testPassword = "test";
		const int passwordLength = 4;

		var specialChars = new[] { "*", "?", "|", "$" };

		foreach (var specialChar in specialChars)
		{
			// Create a PasswordBox with special PasswordChar
			var passwordBox = new PasswordBox
			{
				PasswordChar = specialChar,
				Password = testPassword,
				FontSize = 16,
				Width = 100,
				Height = 32,
				Padding = new Thickness(4)
			};

			// Create a TextBox with the same special characters for comparison (spell-check off, as above)
			var textBox = new TextBox
			{
				Text = new string(specialChar[0], passwordLength),
				IsSpellCheckEnabled = false,
				FontSize = 16,
				Width = 100,
				Height = 32,
				Padding = new Thickness(4)
			};

			// Load PasswordBox and take screenshot
			await UITestHelper.Load(passwordBox);
			var passwordBoxScreenshot = await UITestHelper.ScreenShot(passwordBox);

			// Load TextBox and take screenshot
			await UITestHelper.Load(textBox);
			var textBoxScreenshot = await UITestHelper.ScreenShot(textBox);

			// Compare visual appearance
			await ImageAssert.AreSimilarAsync(passwordBoxScreenshot, textBoxScreenshot, imperceptibilityThreshold: 0.05);
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public void When_PasswordChar_Set()
	{
		var passwordBox = new PasswordBox();

#if !HAS_UNO
		string defaultPasswordBoxChar = "\u25CF";
#else
		string defaultPasswordBoxChar = PasswordBox.DefaultPasswordChar;
#endif
		// Test default value
		Assert.AreEqual(defaultPasswordBoxChar, passwordBox.PasswordChar);

		// Test setting custom value
		passwordBox.PasswordChar = "*";
		Assert.AreEqual("*", passwordBox.PasswordChar);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public void When_PasswordChar_Set_To_Invalid()
	{
		string[] invalidValues = ["", null, "AB", "LongString"];
		foreach (var invalid in invalidValues)
		{
			var passwordBox = new PasswordBox();
			Assert.ThrowsExactly<ArgumentException>(() => passwordBox.PasswordChar = invalid);
		}
	}

#if HAS_UNO
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia & ~RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Copy_Cut_Does_Not_Leak_Password()
	{
		if (!Uno.Foundation.Extensibility.ApiExtensibility.IsRegistered<ApplicationModel.DataTransfer.IClipboardExtension>())
		{
			Assert.Inconclusive("Platform does not support clipboard operations.");
		}

		const string sentinel = "clipboard-sentinel";
		const string secret = "hunter2";

		var seed = new DataPackage();
		seed.SetText(sentinel);
		Clipboard.SetContent(seed);
		await TestServices.WindowHelper.WaitForIdle();

		var passwordBox = new PasswordBox
		{
			Password = secret,
			Width = 150
		};

		await UITestHelper.Load(passwordBox);

		passwordBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();
		passwordBox.SelectAll();
		await TestServices.WindowHelper.WaitForIdle();

		// Driven through the keyboard rather than CopySelectionToClipboard/CutSelectionToClipboard: those are
		// TextBox-only API that a PasswordBox no longer exposes, and the accelerator is the path a user has.
		var ctrl = DeviceTargetHelper.PlatformCommandModifier;

		passwordBox.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(passwordBox, VirtualKey.C, ctrl, unicodeKey: 'c'));
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(sentinel, await ClipboardHelper.WaitForTextAsync(sentinel), "Ctrl+C must not put the password on the clipboard");

		passwordBox.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(passwordBox, VirtualKey.X, ctrl, unicodeKey: 'x'));
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(sentinel, await ClipboardHelper.WaitForTextAsync(sentinel), "Ctrl+X must not put the password on the clipboard");
		Assert.AreEqual(secret, passwordBox.Password, "Ctrl+X must not remove the selected password");
	}
#endif

	// The hierarchy assertions below are WinUI-parity claims, not Uno implementation details, so they are
	// deliberately not platform-gated: they hold on native WinUI too and should keep holding there.

	[TestMethod]
	public void When_Derives_From_Control_Not_TextBox()
	{
		Assert.AreEqual(typeof(Control), typeof(PasswordBox).BaseType, "WinUI declares `runtimeclass PasswordBox : Control`");
		Assert.IsNotInstanceOfType<TextBox>(new PasswordBox(), "`is TextBox` must be false for a PasswordBox");
	}

	[TestMethod]
	public void When_TextBox_Only_Api_Is_Not_Reachable()
	{
		var type = typeof(PasswordBox);

		// Text is the one that mattered: it used to be a live mirror of the password.
		string[] properties =
		[
			"Text", "SelectedText", "SelectionStart", "SelectionLength", "IsReadOnly", "AcceptsReturn",
			"TextWrapping", "CharacterCasing", "IsSpellCheckEnabled", "IsTextPredictionEnabled",
			"TextAlignment", "PlaceholderForeground", "CanUndo", "CanRedo", "ProofingMenuFlyout",
		];
		foreach (var name in properties)
		{
			Assert.IsNull(type.GetProperty(name), $"PasswordBox must not expose {name}");
		}

		string[] methods = ["Select", "CopySelectionToClipboard", "CutSelectionToClipboard", "Undo", "Redo", "ClearUndoRedoHistory"];
		foreach (var name in methods)
		{
			Assert.IsNull(type.GetMethod(name), $"PasswordBox must not expose {name}()");
		}

		string[] events = ["TextChanged", "TextChanging", "BeforeTextChanging", "SelectionChanged", "SelectionChanging"];
		foreach (var name in events)
		{
			Assert.IsNull(type.GetEvent(name), $"PasswordBox must not expose {name}");
		}
	}

	[TestMethod]
	public void When_WinUI_Surface_Is_Declared()
	{
		var type = typeof(PasswordBox);

		string[] properties =
		[
			"Password", "PasswordChar", "PasswordRevealMode", "IsPasswordRevealButtonEnabled", "MaxLength",
			"Header", "HeaderTemplate", "PlaceholderText", "SelectionHighlightColor", "InputScope",
			"CanPasteClipboardContent", "SelectionFlyout", "Description",
		];
		foreach (var name in properties)
		{
			Assert.IsNotNull(type.GetProperty(name), $"PasswordBox must expose {name}");
			Assert.IsNotNull(type.GetProperty($"{name}Property"), $"PasswordBox must expose {name}Property");
		}

		foreach (var name in new[] { "SelectAll", "PasteFromClipboard" })
		{
			Assert.IsNotNull(type.GetMethod(name), $"PasswordBox must expose {name}()");
		}

		foreach (var name in new[] { "PasswordChanged", "ContextMenuOpening", "Paste" })
		{
			Assert.IsNotNull(type.GetEvent(name), $"PasswordBox must expose {name}");
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Password_Is_Not_Reachable_As_Text()
	{
		const string secret = "hunter2";
		var SUT = new PasswordBox { Password = secret, Width = 150 };
		await UITestHelper.Load(SUT);

		// Every readable string-valued member on the instance, not a hand-picked list: the point is that no
		// reachable member returns the cleartext, including any added later.
		foreach (var property in typeof(PasswordBox).GetProperties())
		{
			if (property.PropertyType != typeof(string) || property.GetGetMethod() is null || property.Name == nameof(PasswordBox.Password))
			{
				continue;
			}

			string value;
			try
			{
				value = (string)property.GetValue(SUT);
			}
			catch
			{
				continue; // NotImplemented stubs raise rather than return.
			}

			Assert.AreNotEqual(secret, value, $"{property.Name} leaks the password");
		}

		Assert.AreEqual(secret, SUT.Password, "Password itself must still round-trip");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Password_Changes_PasswordChanged_Is_Raised()
	{
		var SUT = new PasswordBox { Width = 150 };
		await UITestHelper.Load(SUT);

		var raised = 0;
		SUT.PasswordChanged += (_, _) => raised++;

		SUT.Password = "one";
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(1, raised);

		SUT.Password = "two";
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(2, raised);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_MaxLength_Matches_TextBox()
	{
		// Asserted as parity with TextBox rather than against a hard-coded result: MaxLength has to keep
		// working now that Password carries its own coercion instead of borrowing Text's, and whichever
		// semantic the platform picks for an over-long programmatic value, both controls must agree.
		var reference = new TextBox { MaxLength = 4, Width = 150 };
		var SUT = new PasswordBox { MaxLength = 4, Width = 150 };

		var panel = new StackPanel();
		panel.Children.Add(reference);
		panel.Children.Add(SUT);
		await UITestHelper.Load(panel);

		reference.Text = "0123456789";
		SUT.Password = "0123456789";
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(reference.Text, SUT.Password, "an over-long value must be handled the same as on TextBox");

		reference.Text = "012";
		SUT.Password = "012";
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual("012", SUT.Password, "a value within MaxLength must be accepted");
	}

#if HAS_UNO
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Automation_Does_Not_Expose_Password()
	{
		const string secret = "hunter2";
		var SUT = new PasswordBox { Password = secret, Width = 150 };
		await UITestHelper.Load(SUT);

		Assert.IsNull(SUT.GetAccessibilityInnerText(), "the accessibility inner text must not carry the password");

		if (FrameworkElementAutomationPeer.CreatePeerForElement(SUT) is not PasswordBoxAutomationPeer peer)
		{
			Assert.Fail("PasswordBox must create a PasswordBoxAutomationPeer");
			return;
		}

		Assert.IsTrue(peer.IsPassword(), "the peer must report IsPassword so screen readers announce it as protected");

		var value = ((IValueProvider)peer).Value;
		Assert.AreNotEqual(secret, value, "UIA Value must not carry the password");
		Assert.AreEqual(secret.Length, value.Length, "UIA Value must be masked to the password's length");
	}
#endif
}
