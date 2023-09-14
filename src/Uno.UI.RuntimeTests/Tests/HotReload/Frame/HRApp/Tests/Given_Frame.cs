#nullable disable

using System;
using System.Formats.Asn1;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.Disposables;
using Uno.UI.RemoteControl;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;
using Uno.UI.RuntimeTests.Tests.HotReload;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_Frame
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

			var frame = new Windows.UI.Xaml.Controls.Frame();
			UnitTestsUIContentHelper.Content = frame;

			frame.Navigate(typeof(HR_Frame_Pages_Page1));

			var message = new HR_Frame_Pages_Page1().CreateUpdateFileMessage(
				originalText: FirstPageTextBlockOriginalText,
				replacementText: FirstPageTextBlockChangedText);

			// Check the initial text of the TextBlock
			await frame.ValidateFirstTextBlockOnCurrentPageText(message.OriginalXaml);
		}

		/// <summary>
		/// Checks that a simple change to a XAML element (change Text on TextBlock) will be applied to
		/// the currently visible page:
		/// Open Page1
		/// Change Page1
		/// </summary>
		[TestMethod]
		public async Task Check_Can_Change_Page1()
		{
			var ct = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

			var frame = new Windows.UI.Xaml.Controls.Frame();
			UnitTestsUIContentHelper.Content = frame;

			frame.Navigate(typeof(HR_Frame_Pages_Page1));

			// Check the initial text of the TextBlock
			await frame.ValidateFirstTextBlockOnCurrentPageText(FirstPageTextBlockOriginalText);

			// Check the updated text of the TextBlock
			await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_Page1>(
				FirstPageTextBlockOriginalText,
				FirstPageTextBlockChangedText,
				() => frame.ValidateFirstTextBlockOnCurrentPageText(FirstPageTextBlockChangedText),
				ct);
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

			var frame = new Windows.UI.Xaml.Controls.Frame();
			UnitTestsUIContentHelper.Content = frame;

			frame.Navigate(typeof(HR_Frame_Pages_Page1));

			// Check the initial text of the TextBlock
			await frame.ValidateFirstTextBlockOnCurrentPageText(FirstPageTextBlockOriginalText);

			await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_Page1>(
				FirstPageTextBlockOriginalText,
				FirstPageTextBlockChangedText,
				async () =>
				{
					// Check to make sure the TextBlock was updated (see Check_Can_Change_Page1 for this test)
					await frame.ValidateFirstTextBlockOnCurrentPageText(FirstPageTextBlockChangedText);

					// Navigate to the second page, verify navigation worked, and then navigate back
					frame.Navigate(typeof(HR_Frame_Pages_Page2));
					await frame.ValidateFirstTextBlockOnCurrentPageText(SecondPageTextBlockOriginalText);
					frame.GoBack();

					// Validate again that the TextBlock still has the updated value
					await frame.ValidateFirstTextBlockOnCurrentPageText(FirstPageTextBlockChangedText);
				},
				ct);

			// Check that after the test has executed, the xaml is back to the original text
			await frame.ValidateFirstTextBlockOnCurrentPageText(FirstPageTextBlockOriginalText);
		}


		/// <summary>
		/// Checks that a simple xaml change to the a page that is not currently visible will be 
		/// applied when the page is navigated to
		/// Open Page1
		/// Change Page2
		/// Navigate to Page2
		/// </summary>
		[TestMethod]
		[Ignore("Not yet working")]
		public async Task Check_Can_Change_Page2_Before_Navigation()
		{
			var ct = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

			var frame = new Windows.UI.Xaml.Controls.Frame();
			UnitTestsUIContentHelper.Content = frame;

			// Navigate to Page1
			frame.Navigate(typeof(HR_Frame_Pages_Page1));

			// Check the initial text of the TextBlock
			await frame.ValidateFirstTextBlockOnCurrentPageText(FirstPageTextBlockOriginalText);

			await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_Page2>(
				SecondPageTextBlockOriginalText,
				SecondPageTextBlockChangedText,
				async () =>
				{
					// Check to make sure the current page wasn't changed
					await frame.ValidateFirstTextBlockOnCurrentPageText(FirstPageTextBlockOriginalText);

					// Navigate to the second page, verify the TextBlock on the second page has the updated value
					//frame.Navigate(typeof(HR_Frame_Pages_Page2));
					(frame.Content as HR_Frame_Pages_Page1)?.Page2Click(null, null);
					await frame.ValidateFirstTextBlockOnCurrentPageText(SecondPageTextBlockChangedText);

					// Go back and Validate again that the TextBlock still has same value
					frame.GoBack();
					await frame.ValidateFirstTextBlockOnCurrentPageText(FirstPageTextBlockOriginalText);
				},
				ct);

			// Check that after the test has executed, the xaml is back to the original text
			await frame.ValidateFirstTextBlockOnCurrentPageText(FirstPageTextBlockOriginalText);
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

			var frame = new Windows.UI.Xaml.Controls.Frame();
			UnitTestsUIContentHelper.Content = frame;

			// Navigate to Page1
			frame.Navigate(typeof(HR_Frame_Pages_Page1));

			// Check the initial text of the TextBlock
			await frame.ValidateFirstTextBlockOnCurrentPageText(FirstPageTextBlockOriginalText);

			// Navigate to the second page, verify the TextBlock on the second page has the updated value
			frame.Navigate(typeof(HR_Frame_Pages_Page2));
			await frame.ValidateFirstTextBlockOnCurrentPageText(SecondPageTextBlockOriginalText);

			await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_Page1>(
				FirstPageTextBlockOriginalText,
				FirstPageTextBlockChangedText,
				async () =>
				{
					// Check to make sure the current page wasn't changed
					await frame.ValidateFirstTextBlockOnCurrentPageText(SecondPageTextBlockOriginalText);

					// Go back and Validate again that the TextBlock has changed value
					frame.GoBack();
					await frame.ValidateFirstTextBlockOnCurrentPageText(FirstPageTextBlockChangedText);
				},
				ct);

			// Check that after the test has executed, the xaml is back to the original text
			await frame.ValidateFirstTextBlockOnCurrentPageText(FirstPageTextBlockOriginalText);
		}

	}
}
