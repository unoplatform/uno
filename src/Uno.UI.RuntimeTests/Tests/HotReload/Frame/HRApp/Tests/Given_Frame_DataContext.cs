#nullable disable
#pragma warning disable IDE0051 // Members used for testing by reflection

using System;
using System.Formats.Asn1;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.Disposables;
using Uno.UI.RemoteControl;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests.Pages;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_Frame_DataContext : BaseTestClass
{
	private const string SimpleTextChange = " (changed)";
	private const string VMText = " VM Text";

	private const string FrameTextBlockOriginalText = "Frame";
	private const string FrameVMText = FrameTextBlockOriginalText + VMText;

	private const string FirstPageTextBlockOriginalText = "First page";
	private const string FirstPageTextBlockChangedText = FirstPageTextBlockOriginalText + SimpleTextChange;
	private const string FirstPageVMText = FirstPageTextBlockOriginalText + VMText;

	private const string SecondPageTextBlockOriginalText = "Second page";
	private const string SecondPageTextBlockChangedText = SecondPageTextBlockOriginalText + SimpleTextChange;
	private const string SecondPageVMText = SecondPageTextBlockOriginalText + VMText;

	/// <summary>
	/// Checks that a simple change to a XAML element (change Text on TextBlock) will be applied to
	/// the currently visible page:
	/// Open Page1
	/// Change Page1
	/// </summary>
	[TestMethod]
	public async Task Check_Can_Change_Page1_With_DataContext()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

		var frame = new Windows.UI.Xaml.Controls.Frame();
		UnitTestsUIContentHelper.Content = frame;

		frame.Navigate(typeof(HR_Frame_Pages_Page1));

		var vm = new HR_Frame_Pages_Page1_VM(FirstPageVMText);
		(frame.Content as Page).DataContext = vm;

		// Check the initial text of the TextBlock
		await frame.ValidateTextOnChildTextBlock(vm.TitleText, 1);

		// Check the text of the TextBlock doesn't change
		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_Page1>(
			FirstPageTextBlockOriginalText,
			FirstPageTextBlockChangedText,
			() => frame.ValidateTextOnChildTextBlock(vm.TitleText, 1),
			ct);

		// Check that the text is still the original value
		await frame.ValidateTextOnChildTextBlock(vm.TitleText, 1);
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
	public async Task Check_Can_Change_Page1_Navigate_And_Return_With_DataContext()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

		var frame = new Windows.UI.Xaml.Controls.Frame();
		UnitTestsUIContentHelper.Content = frame;

		frame.Navigate(typeof(HR_Frame_Pages_Page1));

		var vm = new HR_Frame_Pages_Page1_VM(FirstPageVMText);
		(frame.Content as Page).DataContext = vm;

		// Check the initial text of the TextBlock
		await frame.ValidateTextOnChildTextBlock(vm.TitleText, 1);

		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_Page1>(
			FirstPageTextBlockOriginalText,
			FirstPageTextBlockChangedText,
			async () =>
			{
				// Check to make sure the TextBlock was updated (see Check_Can_Change_Page1 for this test)
				await frame.ValidateTextOnChildTextBlock(vm.TitleText, 1);

				// Navigate to the second page, verify navigation worked, and then navigate back
				frame.Navigate(typeof(HR_Frame_Pages_Page2));
				await frame.ValidateTextOnChildTextBlock(SecondPageTextBlockOriginalText);
				frame.GoBack();

				// Validate again that the TextBlock still has the updated value
				await frame.ValidateTextOnChildTextBlock(vm.TitleText, 1);
			},
			ct);

		// Check that after the test has executed, the xaml is back to the original text
		await frame.ValidateTextOnChildTextBlock(vm.TitleText, 1);
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
	//[Ignore("Not yet working")]
	public async Task Check_Can_Change_Page1_Before_Navigating_Back_With_DataContext()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

		var frame = new Windows.UI.Xaml.Controls.Frame();
		UnitTestsUIContentHelper.Content = frame;

		// Navigate to Page1
		frame.Navigate(typeof(HR_Frame_Pages_Page1));

		var vm = new HR_Frame_Pages_Page1_VM(FirstPageVMText);
		(frame.Content as Page).DataContext = vm;

		// Check the initial text of the TextBlock
		await frame.ValidateTextOnChildTextBlock(vm.TitleText, 1);

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
				await frame.ValidateTextOnChildTextBlock(vm.TitleText, 1);
			},
			ct);

		// Check that after the test has executed, the xaml is back to the original text
		await frame.ValidateTextOnChildTextBlock(vm.TitleText, 1);
	}


	[TestMethod]
	public async Task Check_Can_Change_Page1_With_Inherited_DataContext()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

		var frame = new Windows.UI.Xaml.Controls.Frame();
		UnitTestsUIContentHelper.Content = frame;

		frame.Navigate(typeof(HR_Frame_Pages_Page1));

		var frame_vm = new HR_Frame_VM(FrameVMText);
		frame.DataContext = frame_vm;
		var vm = new HR_Frame_Pages_Page1_VM(FirstPageVMText);
		(frame.Content as Page).DataContext = vm;

		// Check the initial text of the TextBlock
		await frame.ValidateTextOnChildTextBlock(vm.TitleText, 1);

		// Check the text of the TextBlock doesn't change
		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_Page1>(
			FirstPageTextBlockOriginalText,
			FirstPageTextBlockChangedText,
			() => frame.ValidateTextOnChildTextBlock(vm.TitleText, 1),
			ct);

		// Check that the text is still the original value
		await frame.ValidateTextOnChildTextBlock(vm.TitleText, 1);
	}
}
