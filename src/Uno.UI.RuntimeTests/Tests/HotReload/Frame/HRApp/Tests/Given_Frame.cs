
using System.Reflection.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.UI.Helpers;
using Uno.UI;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_Frame : BaseTestClass
{
	public const string SimpleTextChange = " (changed)";

	public const string FirstPageTextBlockOriginalText = "First page";
	public const string FirstPageTextBlockChangedText = FirstPageTextBlockOriginalText + SimpleTextChange;

	public const string SecondPageTextBlockOriginalText = "Second page";
	public const string SecondPageTextBlockChangedText = SecondPageTextBlockOriginalText + SimpleTextChange;


	/// <summary>
	/// No Change to Page 1 - just loads Page 1 without triggering any HR changes
	/// Useful for doing manual xaml changes and validating HR is applied
	/// </summary>
	[TestMethod]
	public async Task Check_NoChange_Page1()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

		var frame = new Microsoft.UI.Xaml.Controls.Frame();
		UnitTestsUIContentHelper.Content = frame;

		frame.Navigate(typeof(HR_Frame_Pages_Page1));

		var message = new HR_Frame_Pages_Page1().CreateUpdateFileMessage(
			originalText: FirstPageTextBlockOriginalText,
			replacementText: FirstPageTextBlockChangedText);

		// Check the initial text of the TextBlock
		await frame.ValidateTextOnChildTextBlock(message.OldText);
	}

	/// <summary>
	/// Checks that a simple change to a XAML element (change Text on TextBlock) will be applied to
	/// the currently visible page:
	/// Open Page1
	/// Change Page1
	/// </summary>
	[TestMethod]
	public async Task Check_Can_Change_Page1_NoPause()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

		var frame = new Microsoft.UI.Xaml.Controls.Frame();
		UnitTestsUIContentHelper.Content = frame;

		frame.Navigate(typeof(HR_Frame_Pages_Page1));

		// Check the initial text of the TextBlock
		await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);

		// Check the updated text of the TextBlock
		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_Page1>(
			FirstPageTextBlockOriginalText,
			FirstPageTextBlockChangedText,
			() => frame.ValidateTextOnChildTextBlock(FirstPageTextBlockChangedText),
			ct);

		// Validate that the page has been returned to the original text
		await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);
	}

	/// <summary>
	/// Checks that a simple change to a XAML element (change Text on TextBlock) will not be applied to
	/// the currently visible page when HR is paused and that the change will be applied once HR is resumed
	/// Open Page1
	/// Pause HR
	/// Change Page1 (no changes to Page1 UI)
	/// Resume HR (changes applied to Page1 UI)
	/// </summary>
	[TestMethod]
	public async Task Check_Can_Change_Page1_Pause_HR()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

		var frame = new Microsoft.UI.Xaml.Controls.Frame();
		UnitTestsUIContentHelper.Content = frame;

		frame.Navigate(typeof(HR_Frame_Pages_Page1));

		// Check the initial text of the TextBlock
		await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);

		// Pause HR
		TypeMappings.Pause();
		try
		{

			// Check the text of the TextBlock is the same even after a HR change (since HR is paused)
			await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_Page1>(
				FirstPageTextBlockOriginalText,
				FirstPageTextBlockChangedText,
				async () =>
				{
					await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);
				},
				ct);
		}
		finally
		{
			// Resume HR
			TypeMappings.Resume();
		}

		// Check that the text has been updated
		await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);
	}


	/// <summary>
	/// Checks that a simple change to a XAML element (change Text on TextBlock) will not be applied to
	/// the currently visible page when HR is paused and that the change will be applied once HR is resumed
	/// Open Page1
	/// Pause HR
	/// Change Page1 (no changes to Page1 UI)
	/// Resume HR (changes applied to Page1 UI)
	/// </summary>
	[TestMethod]
	public async Task Check_Can_Change_Page1_Pause_NoUIUpdate_HR()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

		var frame = new Microsoft.UI.Xaml.Controls.Frame();
		UnitTestsUIContentHelper.Content = frame;

		frame.Navigate(typeof(HR_Frame_Pages_Page1));

		// Check the initial text of the TextBlock
		await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);

		// Pause HR
		TypeMappings.Pause();
		try
		{

			// Check the text of the TextBlock is the same even after a HR change (since HR is paused)
			await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_Page1>(
				FirstPageTextBlockOriginalText,
				FirstPageTextBlockChangedText,
				async () =>
				{
					await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);
				},
				ct);
		}
		finally
		{
			// Resume HR
			TypeMappings.Resume(false);
		}

		// Although HR has been un-paused (resumed) the UI should not have updated at this point
		// due to false parameter passed to Resume method
		await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);

		// Force a refresh
		Window.Current!.ForceHotReloadUpdate();

		await TestingUpdateHandler.WaitForVisualTreeUpdate().WaitAsync(ct);

		// Check that the text has been updated
		await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);
	}


	/// <summary>
	/// Checks that a simple change to a XAML element (change Text on TextBlock) will not be applied to
	/// the currently visible page when HR is paused and that the change will be applied once HR is resumed
	/// Open Page1
	/// Pause HR
	/// Change Page1 (no changes to Page1 UI)
	/// Resume HR (changes applied to Page1 UI)
	/// </summary>
	[TestMethod]
	[Ignore]
	public async Task Check_Can_Change_Page1_Pause_ReloadCompleted_HR()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

		var frame = new Microsoft.UI.Xaml.Controls.Frame();
		UnitTestsUIContentHelper.Content = frame;

		frame.Navigate(typeof(HR_Frame_Pages_Page1));

		// Check the initial text of the TextBlock
		await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);

		// Pause HR
		TypeMappings.Pause();
		try
		{
			var waitingTask = TestingUpdateHandler.WaitForReloadCompleted();

			// Check the text of the TextBlock is the same even after a HR change (since HR is paused)
			await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_Page1>(
				FirstPageTextBlockOriginalText,
				FirstPageTextBlockChangedText,
				async () =>
				{
					// Confirm that reload compeleted has fired
					var uiUpdated = await waitingTask.WaitAsync(ct);
					Assert.IsFalse(uiUpdated, "UI should not have updated whilst ui updates paused");
					await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);
				},
				ct);
		}
		finally
		{
			// Resume HR
			TypeMappings.Resume(false);
		}

		// Although HR has been un-paused (resumed) the UI should not have updated at this point
		// due to false parameter passed to Resume method
		await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);

		// Force a refresh
		Window.Current!.ForceHotReloadUpdate();

		await TestingUpdateHandler.WaitForVisualTreeUpdate().WaitAsync(ct);

		// Check that the text has been updated
		await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);
	}


	/// <summary>
	/// Checks that a simple xaml change to the current page will be retained when
	/// navigating forward to a new page and then going back to the original page	
	/// Open Page1
	/// Change Page1
	/// Navigate to Page2
	/// Navigate back to Page1
	/// </summary>
	[TestMethod]
	public async Task Check_Can_Change_Page1_Navigate_And_Return()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

		var frame = new Microsoft.UI.Xaml.Controls.Frame();
		UnitTestsUIContentHelper.Content = frame;

		frame.Navigate(typeof(HR_Frame_Pages_Page1));

		// Check the initial text of the TextBlock
		await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);

		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_Page1>(
			FirstPageTextBlockOriginalText,
			FirstPageTextBlockChangedText,
			async () =>
			{
				// Check to make sure the TextBlock was updated (see Check_Can_Change_Page1 for this test)
				await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockChangedText);

				// Navigate to the second page, verify navigation worked, and then navigate back
				frame.Navigate(typeof(HR_Frame_Pages_Page2));
				await frame.ValidateTextOnChildTextBlock(SecondPageTextBlockOriginalText);
				frame.GoBack();

				// Validate again that the TextBlock still has the updated value
				await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockChangedText);
			},
			ct);

		// Check that after the test has executed, the xaml is back to the original text
		await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);
	}


	/// <summary>
	/// Checks that a simple xaml change to the a page that is not currently visible will be 
	/// applied when the page is navigated to
	/// Open Page1
	/// Change Page2
	/// Navigate to Page2
	/// </summary>
	[TestMethod]
	public async Task Check_Can_Change_Page2_Before_Navigation()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

		var frame = new Microsoft.UI.Xaml.Controls.Frame();
		UnitTestsUIContentHelper.Content = frame;

		// Navigate to Page1
		frame.Navigate(typeof(HR_Frame_Pages_Page1));

		// Check the initial text of the TextBlock
		await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);

		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_Page2>(
			SecondPageTextBlockOriginalText,
			SecondPageTextBlockChangedText,
			async () =>
			{
				// Check to make sure the current page wasn't changed
				await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);

				// Navigate to the second page, verify the TextBlock on the second page has the updated value
				(frame.Content as HR_Frame_Pages_Page1)?.Page2Click(this, new RoutedEventArgs());

				await frame.ValidateTextOnChildTextBlock(SecondPageTextBlockChangedText);

				// Go back and Validate again that the TextBlock still has same value
				frame.GoBack();
				await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);
			},
			ct);

		// Check that after the test has executed, the xaml is back to the original text
		await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);
	}

	/// <summary>
	/// Checks that a simple xaml change to the a page that is in the backstack will be
	/// applied when the page is navigated back to
	/// Open Page1
	/// Navigate to Page2
	/// Change Page1
	/// Navigate back to Page1
	/// </summary>
	[TestMethod]
	public async Task Check_Can_Change_Page1_Before_Navigating_Back()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

		var frame = new Microsoft.UI.Xaml.Controls.Frame();
		UnitTestsUIContentHelper.Content = frame;

		// Navigate to Page1
		frame.Navigate(typeof(HR_Frame_Pages_Page1));

		// Check the initial text of the TextBlock
		await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);

		// Navigate to the second page, verify the TextBlock on the second page has the updated value
		frame.Navigate(typeof(HR_Frame_Pages_Page2));
		await frame.ValidateTextOnChildTextBlock(SecondPageTextBlockOriginalText);

		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_Page1>(
			FirstPageTextBlockOriginalText,
			FirstPageTextBlockChangedText,
			async () =>
			{
				// Check to make sure the current page wasn't changed
				await frame.ValidateTextOnChildTextBlock(SecondPageTextBlockOriginalText);

				// Go back and Validate again that the TextBlock has changed value
				frame.GoBack();
				await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockChangedText);
			},
			ct);

		// Check that after the test has executed, the xaml is back to the original text
		await frame.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);
	}

}
