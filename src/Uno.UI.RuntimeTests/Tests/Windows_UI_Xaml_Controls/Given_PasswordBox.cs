using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Uno.UI.RuntimeTests.Helpers;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_PasswordBox
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

		// Create a TextBox with "AAAA" to compare visual appearance
		var textBox = new TextBox
		{
			Text = new string('A', passwordLength),
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

			// Create a TextBox with the same special characters for comparison
			var textBox = new TextBox
			{
				Text = new string(specialChar[0], passwordLength),
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

		// Test default value
		Assert.AreEqual(PasswordBox.DefaultPasswordChar, passwordBox.PasswordChar);

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
}
