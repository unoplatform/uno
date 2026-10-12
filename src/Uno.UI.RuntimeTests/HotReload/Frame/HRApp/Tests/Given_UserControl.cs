
using System.Reflection.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.UI.Helpers;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_UserControl : BaseTestClass
{
	public const string SimpleTextChange = " (changed)";

	public const string UserControl1TextBlockOriginalText = "Control 1";
	public const string UserControl1TextBlockChangedText = UserControl1TextBlockOriginalText + SimpleTextChange;

	/// <summary>
	/// Change the Text of a TextBlock inside a UserControl
	/// </summary>
	[TestMethod]
	public async Task Check_Change_UserControl()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

		var frame = new Microsoft.UI.Xaml.Controls.Frame();
		UnitTestsUIContentHelper.Content = frame;

		frame.Navigate(typeof(HR_Frame_Pages_Page1));

		// Check the initial text of the TextBlock
		await frame.ValidateTextOnChildTextBlock(UserControl1TextBlockOriginalText, 2); // The user control textblock is the third one on page1

		// Check the updated text of the TextBlock
		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_UC1>(
			UserControl1TextBlockOriginalText,
			UserControl1TextBlockChangedText,
			() => frame.ValidateTextOnChildTextBlock(UserControl1TextBlockChangedText, 2),
			ct);

		// Validate that the page has been returned to the original text
		await frame.ValidateTextOnChildTextBlock(UserControl1TextBlockOriginalText, 2);

		await Task.Yield();
	}


	/// <summary>
	/// Change the Text of a TextBlock inside a UserControl and validate the
	/// DebugParseContext is still correct
	/// </summary>
	[TestMethod]
	public async Task Check_Change_UserControl_DebugPareContext()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

		var frame = new Microsoft.UI.Xaml.Controls.Frame();
		UnitTestsUIContentHelper.Content = frame;

		frame.Navigate(typeof(HR_Frame_Pages_Page1));

		FrameworkElement.DebugParseContextDetails? existingContext = default;

		// Retrieve the existing DebugParseContext
		await frame.ValidateChildElement<HR_Frame_Pages_UC1>(
			async uc =>
			{
				existingContext = uc.DebugParseContext;
				Assert.IsNotNull(existingContext);
				await Task.CompletedTask;
			},
			0
		);

		// Check the updated text of the TextBlock
		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_UC1>(
			UserControl1TextBlockOriginalText,
			UserControl1TextBlockChangedText,
			() => frame.ValidateTextOnChildTextBlock(UserControl1TextBlockChangedText, 2),
			ct);

		// Validate that the DebugParseContext is still correct
		await frame.ValidateChildElement<HR_Frame_Pages_UC1>(
			async uc =>
			{
				var updatedContext = uc.DebugParseContext;
				Assert.IsNotNull(updatedContext);
				if (existingContext is { } && updatedContext is { })
				{
					Assert.AreEqual(existingContext.LocalFileUri, updatedContext.LocalFileUri, "DebugParseContext: LocalFileUri should match");
					Assert.AreEqual(existingContext.LineNumber, updatedContext.LineNumber, "DebugParseContext: LineNumber should match");
					Assert.AreEqual(existingContext.LinePosition, updatedContext.LinePosition, "DebugParseContext: LinePosition should match");
				}
				await Task.CompletedTask;
			},
			0
		);

		await Task.Yield();
	}

	/// <summary>
	/// Change the Text of a TextBlock inside a UserControl, where the UserControl 
	/// is nested inside a Viewbox
	/// </summary>
	[TestMethod]
	public async Task Check_Change_UserControl_Inside_Viewbox()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

		var frame = new Microsoft.UI.Xaml.Controls.Frame();
		UnitTestsUIContentHelper.Content = frame;

		frame.Navigate(typeof(HR_Frame_Pages_Page2));

		// Check the initial text of the TextBlock
		await frame.ValidateTextOnChildTextBlock(UserControl1TextBlockOriginalText, 2); // The user control textblock is the third one on page1

		// Check the updated text of the TextBlock
		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_UC1>(
			UserControl1TextBlockOriginalText,
			UserControl1TextBlockChangedText,
			() => frame.ValidateTextOnChildTextBlock(UserControl1TextBlockChangedText, 2),
			ct);

		// Validate that the page has been returned to the original text
		await frame.ValidateTextOnChildTextBlock(UserControl1TextBlockOriginalText, 2);
		await Task.Yield();
	}

}
